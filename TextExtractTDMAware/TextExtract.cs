using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Datalogics.PDFL;

/*
 * This is a TDM-aware(*) version of TextExtract, a program which pulls text from a PDF file and 
 * exports it to a text file (TXT). It will open a PDF file called Constitution.PDF and create 
 * an output file called tdmAwareTextExtract-untagged-out.txt. The export file includes page 
 * number references, and the text is produced using standard Times Roman encoding. The program 
 * is also written to include a provision for working with tagged documents, and determines if 
 * the original PDF file is tagged or untagged. Tagging is used to make PDF files accessible
 * to the blind or to people with vision problems. 
 *
 * (*) "TDM-aware" meaning that it looks for TDM Reservation Metadata in the input document and,
 * if found and accessible, prints that information to console.
 * 
 * Copyright (c) 2007-2025, Datalogics, Inc. All rights reserved.
 *
 */
namespace TextExtract
{


    class tdmAwareTextExtract
    {

        static void Main(string[] args)
        {
            Console.WriteLine("tdmAwareTextExtract Sample:");

            using (Library lib = new Library())
            {
                Console.WriteLine("Initialized the library.");

                // This is a tagged PDF.
                String sInput = Library.ResourceDirectory + "Sample_Input/Constitution.pdf";

                if (args.Length > 0)
                    sInput = args[0];

                // This is an untagged PDF.
                //Resources/Sample_Input/constitution.pdf"

                Document doc = new Document(sInput);
                Console.WriteLine("Input file:  " + sInput);

                bool bCanProceed = false;
                // For Reference: https://www.w3.org/community/reports/tdmrep/CG-FINAL-tdmrep-20240510/

                const string tdmNS = "http://www.w3.org/ns/tdmrep";
                if (!doc.GetXMPMetadataProperty(tdmNS, "reservation").Equals("1"))
                    bCanProceed = true;
                else
                {
                    var policyURL = doc.GetXMPMetadataProperty(tdmNS, "policy");
                    Uri uriResult;
                    bool validURL = Uri.TryCreate(policyURL, UriKind.Absolute, out uriResult) && uriResult.Scheme == Uri.UriSchemeHttps;
                    // use the below statement if the policy URL in the XMP metadata references a local file (usually for testing purposes)
                    //bool validURL = Uri.TryCreate(policyURL, UriKind.Absolute, out uriResult) && uriResult.Scheme == Uri.UriSchemeFile;
                    if (validURL)
                    {
                        using (var client = new WebClient())
                        using (var memStream = new MemoryStream(client.DownloadData(policyURL)))
                        using (var jsondoc = JsonDocument.Parse(memStream))
                        {
                            //cribbing from: https://kevsoft.net/2021/12/19/traversing-json-with-jsondocument.html
                            // this section prints the vcard info under the "assigner" property of the policy.
                            if (jsondoc.RootElement.GetProperty("@type").GetString() != null &&
                                jsondoc.RootElement.GetProperty("profile").GetString() != null &&
                                jsondoc.RootElement.GetProperty("@type").GetString().Equals("Offer") &&
                                jsondoc.RootElement.GetProperty("profile").GetString().Equals(tdmNS))
                            {
                                foreach (var jsonProperty in jsondoc.RootElement.GetProperty("assigner").EnumerateObject())
                                {
                                    Object currKind = jsonProperty.Value.ValueKind;
                                    try
                                    {
                                        if (currKind == null || currKind.ToString() == "Undefined" || currKind.ToString() == "Null")
                                        {
                                            Console.WriteLine("null or undefined value kind at: " + jsonProperty.Name);
                                            continue;
                                        }

                                        if (currKind.ToString() == "Array")
                                        {
                                            for (int h = 0; h < jsonProperty.Value.GetArrayLength(); h++)
                                                Console.WriteLine($"Assigner: {jsonProperty.Name}[{h}]: {jsonProperty.Value[h]}");
                                        }

                                        else
                                        {
                                            Console.WriteLine($"Assigner: {jsonProperty.Name}: {jsonProperty.Value}");
                                        }
                                    }

                                    catch (System.InvalidOperationException invalidEX)
                                    {
                                        Console.WriteLine("Error: {jsonProperty.Name} {0}", invalidEX.Message);
                                    }
                                }

                                // this section prints the "permission" property of the policy.
                                if (jsondoc.RootElement.GetProperty("permission").ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var jsonArrayElem in jsondoc.RootElement.GetProperty("permission").EnumerateArray())
                                    {
                                        try
                                        {
                                            foreach(JsonProperty prop in jsonArrayElem.EnumerateObject())
                                            {
                                                Console.WriteLine($"Permission: {prop.Name}: {prop.Value}");
                                            }
                                        }

                                        catch (System.InvalidOperationException invalidEX)
                                        {
                                            Console.WriteLine("Error: {jsonProperty.Name} {0}", invalidEX.Message);
                                        }
                                    }
                                }
                                else { Console.WriteLine("Improper policy document. Please refer to: " + tdmNS); }

                                bCanProceed = false;
                            }
                            else { Console.WriteLine("Improper policy document. Please refer to: " + tdmNS); }
                        }
                    }
                }

                if (!bCanProceed)
                {
                    return;
                }

                // Determine if the PDF is tagged.  We'll use a slightly different set of rules 
                // for parsing tagged and untagged PDFs.
                //
                // We'll determine if the PDF is tagged by examining the MarkInfo 
                // dictionary of the document.  First, check for the existence of the MarkInfo dict.
                bool docIsTagged = false;
                PDFDict markInfoDict;
                PDFBoolean markedEntry;
                if ((markInfoDict = (PDFDict)doc.Root.Get("MarkInfo")) != null)
                {
                    if ((markedEntry = (PDFBoolean)markInfoDict.Get("Marked")) != null)
                    {
                        if (markedEntry.Value)
                            docIsTagged = true;
                    }
                }

                WordFinderConfig wordConfig = new WordFinderConfig
                {
                    IgnoreCharGaps = false,
                    IgnoreLineGaps = false,
                    NoAnnots = false,
                    NoEncodingGuess = false,

                    // Std Roman treatment for custom encoding; overrides the noEncodingGuess option
                    UnknownToStdEnc = false,

                    DisableTaggedPDF = false,    // legacy mode WordFinder creation
                    NoXYSort = true,
                    PreserveSpaces = false,
                    NoLigatureExp = false,
                    NoHyphenDetection = false,
                    TrustNBSpace = false,
                    NoExtCharOffset = false,     // text extraction efficiency
                    NoStyleInfo = false         // text extraction efficiency
                };

                WordFinder wordFinder = new WordFinder(doc, WordFinderVersion.Latest, wordConfig);

                if (docIsTagged)
                    ExtractTextTagged(doc, wordFinder);
                else
                    ExtractTextUntagged(doc, wordFinder);
            }
        }

        static void ExtractTextUntagged(Document doc, WordFinder wordFinder)
        {
            int nPages = doc.NumPages;
            IList<Word> pageWords = null;

            System.IO.StreamWriter logfile = new System.IO.StreamWriter("tdmAwareTextExtract-untagged-out.txt");
            Console.WriteLine("Writing tdmAwareTextExtract-untagged-out.txt");

            for (int i = 0; i < nPages; i++)
            {
                pageWords = wordFinder.GetWordList(i);

                String textToExtract = "";

                for (int wordnum = 0; wordnum < pageWords.Count; wordnum++)
                {
                    Word wInfo;
                    wInfo = pageWords[wordnum];
                    string s = wInfo.Text;

                    // Check for hyphenated words that break across a line.  
                    if (((wInfo.Attributes & WordAttributeFlags.HasSoftHyphen) == WordAttributeFlags.HasSoftHyphen) &&
                        ((wInfo.Attributes & WordAttributeFlags.LastWordOnLine) == WordAttributeFlags.LastWordOnLine))
                    {
                        // Remove the hyphen and combine the two parts of the word before adding to the extracted text.
                        // Note that we pass in the Unicode character for soft hyphen as well as the regular hyphen.
                        //
                        // In untagged PDF, it's not uncommon to find a mixture of hard and soft hyphens that may
                        // not be used for their intended purposes.
                        // (Soft hyphens are intended only for words that break across lines.)
                        //
                        // For the purposes of this sample, we'll remove all hyphens.  In practice, you may need to check 
                        // words against a dictionary to determine if the hyphenated word is actually one word or two.
                        // Note we remove ascii hyphen, Unicode soft hyphen(\u00ad) and Unicode hyphen(0x2010).
                        string[] splitstrs = s.Split(new char[] { '-', '\u00ad', '\x2010' });
                        textToExtract += splitstrs[0] + splitstrs[1];
                    }
                    else
                        textToExtract += s;

                    // Check for space adjacency and add a space if necessary.
                    if ((wInfo.Attributes & WordAttributeFlags.AdjacentToSpace) == WordAttributeFlags.AdjacentToSpace)
                    {
                        textToExtract += " ";
                    }
                    // Check for a line break and add one if necessary
                    if ((wInfo.Attributes & WordAttributeFlags.LastWordOnLine) == WordAttributeFlags.LastWordOnLine)
                        textToExtract += "\n";
                }

                logfile.WriteLine("<page " + (i + 1) + ">");
                logfile.WriteLine(textToExtract);

                // Release requested WordList
                for (int wordnum = 0; wordnum < pageWords.Count; wordnum++)
                    pageWords[wordnum].Dispose();
            }
            Console.WriteLine("Extracted " + nPages + " pages.");
            logfile.Close();
        }

        static void ExtractTextTagged(Document doc, WordFinder wordFinder)
        {
            int nPages = doc.NumPages;
            IList<Word> pageWords = null;

            System.IO.StreamWriter logfile = new System.IO.StreamWriter("tdmAwareTextExtract-tagged-out.txt");
            Console.WriteLine("Writing tdmAwareTextExtract-tagged-out.txt");

            for (int i = 0; i < nPages; i++)
            {
                pageWords = wordFinder.GetWordList(i);

                String textToExtract = "";

                for (int wordnum = 0; wordnum < pageWords.Count; wordnum++)
                {
                    Word wInfo;
                    wInfo = pageWords[wordnum];
                    string s = wInfo.Text;

                    // In most tagged PDFs, soft hyphens are used only to break words across lines, so we'll 
                    // check for any soft hyphens and remove them from our text output.
                    // 
                    // Note that we're not checking for the LastWordOnLine flag, unlike untagged PDF.  For Tagged PDF, 
                    // words are not flagged as being the last on the line if they are not at the end of a sentence.
                    if (((wInfo.Attributes & WordAttributeFlags.HasSoftHyphen) == WordAttributeFlags.HasSoftHyphen))
                    {
                        // Remove the hyphen and combine the two parts of the word before adding to the extracted text.
                        // Note that we pass in the Unicode character for soft hyphen(\u00ad) and hyphen(0x2010).
                        string[] splitstrs = s.Split(new char[] { '\u00ad', '\x2010' });
                        textToExtract += splitstrs[0] + splitstrs[1];
                    }
                    else
                        textToExtract += s;

                    // Check for space adjacency and add a space if necessary.
                    if ((wInfo.Attributes & WordAttributeFlags.AdjacentToSpace) == WordAttributeFlags.AdjacentToSpace)
                    {
                        textToExtract += " ";
                    }
                    // Check for a line break and add one if necessary.
                    // Normally this is accomplished using WordAttributeFlags.LastWordOnLine,
                    // but for tagged PDFs, the LastWordOnLine flag is set according to the
                    // tags in the PDF, not according to visual line breaks in the document.
                    //
                    // To preserve the visual line breaks in the document, we'll check whether
                    // the word is the last word in the region.  If you instead prefer to
                    // break lines according to the tags in the PDF, use
                    // (wInfo.Attributes & WordAttributeFlags.LastWordOnLine) == WordAttributeFlags.LastWordOnLine, 
                    // similar to the untagged case.
                    if (wInfo.IsLastWordInRegion)
                        textToExtract += "\n";
                }

                logfile.WriteLine("<page " + (i + 1) + ">");
                logfile.WriteLine(textToExtract);

                // Release requested WordList
                for (int wordnum = 0; wordnum < pageWords.Count; wordnum++)
                    pageWords[wordnum].Dispose();
            }
            Console.WriteLine("Extracted " + nPages + " pages.");
            logfile.Close();
        }
    }
}
