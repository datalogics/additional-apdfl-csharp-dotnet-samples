using Datalogics.PDFL;


/*
 * 
 * This sample demonstrates walking through the field dictionaries of an Acroform PDF.
 * 
 * Copyright (c) 2007-2023, Datalogics, Inc. All rights reserved.
 *
 */

namespace FormWalker
{
    class FormWalker
    {
        static string[] Get_button_info(PDFDict field, bool[] fieldFlags)
        {
            string svalue = "";
            string sdefault_value = "";
            string current_state = "";

            if (fieldFlags[17])
                return new String[] { "Pushbutton" }; //pushbutton is a purely interactive control that responds immediately to user input without retaining a permanent value.

            string buttonType = fieldFlags[16] ? "Radiobutton" : "Checkbox";

            if (field.Contains("V"))
                using (var entry = field.Get("V"))
                    if (entry != null && entry is PDFName)
                        svalue = "Value: " + (entry as PDFName).Value;

            if (field.Contains("DV"))
                using (var entry = field.Get("DV"))
                    if (entry != null && entry is PDFName)
                        sdefault_value = "Default Value: " + (entry as PDFName).Value;

            if (field.Contains("AS"))
                using (var entry = field.Get("AS"))
                    if (entry != null && entry is PDFName)
                        current_state = "Current State: " + (entry as PDFName).Value;

            return new string[] { buttonType, svalue, sdefault_value, current_state };
        }

        static string[] Get_text_info(PDFDict field)
        {
            string svalue;
            string sdefault_value;
            string sMax_Length;
            PDFObject entry;

            entry = field.Get("V");
            if (entry is PDFString)
            {
                var val = entry as PDFString;
                svalue = "Value: " + val.Value;
            }
            else
                svalue = "";

            entry = field.Get("DV");
            if (entry !=null && entry is PDFString)
            {
                var defvalue = entry as PDFString;
                sdefault_value = "Default Value: " + defvalue.Value;
            }
            else
                sdefault_value = "";

            entry = field.Get("MaxLen");
            if (entry != null && entry is PDFInteger)
            {
                var int_entry = entry as PDFInteger;
                int nMax_Length = int_entry.Value;
                sMax_Length = String.Format("Max Length: {0}", nMax_Length);
            }
            else
                sMax_Length = "";

            return new string[] { svalue, sdefault_value, sMax_Length };
        }

        static string[] Get_choice_info(PDFObject field, bool[] fieldFlags)
        {
            string choiceType = fieldFlags[18] ? "Combobox" : "Listbox";

            return new string[] { choiceType, };
        }

        static string[] Get_sigDict_info(PDFDict sigDict)
        {
            String filterName;
            String subFilterName;
            String signerName;
            String contactInfo;
            String location;
            String reason;
            String sigTime;
            String signatureType;

            filterName = (sigDict.Get("Filter") as PDFName).Value;

            // In PDF32000_2008.pdf, see table 252 in section 12.8.1
            // or From Table 8.102 in section 8.7 The following are optional (and by no means a complete listing of the entries in this dictionary).
            signerName = (sigDict.Contains("Name") ? (sigDict.Get("Name") as PDFString).Value : "");
            subFilterName = (sigDict.Contains("SubFilter") ? (sigDict.Get("SubFilter") as PDFName).Value : "");
            reason = (sigDict.Contains("Reason") ? (sigDict.Get("Reason") as PDFString).Value : "");
            contactInfo = (sigDict.Contains("ContactInfo") ? (sigDict.Get("ContactInfo") as PDFString).Value : "");
            sigTime = (sigDict.Contains("M") ? (sigDict.Get("M") as PDFString).Value : "");
            location = (sigDict.Contains("Location") ? (sigDict.Get("Location") as PDFString).Value : "");
            signatureType = (sigDict.Contains("Type") && (sigDict.Get("Type") as PDFName).Value.Equals("DocTimeStamp")) ? "DocTimeStamp" : "";
            return new string[] { filterName, subFilterName, signatureType, sigTime, signerName, contactInfo, location, reason };
        }

        static string[] Get_sig_info(PDFDict field)
        {

            var entry = field.Get("V");
            if (entry != null && entry is PDFDict)
            {
                return Get_sigDict_info(entry as PDFDict);
            }

            return new string[] { "Unsigned", };
        }

        static void Enumerate_field(PDFObject field_entry, string prefix)
        {
            string name_part = "";  //optional
            string field_name;

            if (field_entry is PDFDict)
            {
                var field = field_entry as PDFDict;
                if (field != null && field.Contains("T"))
                    using (var entry = field.Get("T"))
                        if (entry is PDFString)
                        {
                            name_part = (entry as PDFString).Value;
                        }

                if (prefix == "")
                    field_name = name_part;
                else
                    field_name = string.Format("{0}.{1}", prefix, name_part);

                if (field != null && field.Contains("Kids"))
                {
                    using var entry = field.Get("Kids");
                    if (entry is PDFArray)
                    {
                        var kids = entry as PDFArray;
                        for (int i = 0; i < kids.Length; i++)
                            using (var kid_entry = kids.Get(i))
                                Enumerate_field(kid_entry, field_name);
                    }
                }
                else //no kids, so we are at an end-node.
                {
                    string? alternate_name = null;
                    string? mapping_name = null;
                    string field_type;
                    string[] field_info;
                    bool additional_actions = false;
                    bool javascript_formatting = false;
                    bool javascript_calculation = false;
                    bool javascript_validation = false;
                    int field_flags = 0;

                    Console.WriteLine("Name: " + field_name);

                    if (field.Contains("Ff"))
                    {
                        using var entry = field.Get("Ff");
                        if (entry is PDFInteger)
                        {
                            field_flags = (entry as PDFInteger).Value;
                        }
                    }
                    bool[] flags = new bool[28];
                    for (int bitpos = 1; bitpos < flags.Length; bitpos++)
                    {
                        flags[bitpos] = (0 != (field_flags & (0x1 << bitpos - 1)));
                    }

                    using (var entry = field.Get("FT"))
                    {
                        if (entry is PDFName)
                        {
                            switch ((entry as PDFName).Value)
                            {
                                case "Btn": field_type = "Button"; field_info = Get_button_info(field, flags); break;
                                case "Tx": field_type = "Text"; field_info = Get_text_info(field); break;
                                case "Ch": field_type = "Choice"; field_info = Get_choice_info(field, flags); break;
                                case "Sig": field_type = "Signature"; field_info = Get_sig_info(field); break;
                                default: field_type = (entry as PDFName).Value; return;
                            }
                        }
                        else
                        {
                            field_type = "inherited?"; //This entry may be present in a non-terminal field (one whose descendants are fields) to provide an inheritable FT value. However, a non-terminal field does not logically have a type of its own; it is merely a container for inheritable attributes that are intended for descendant terminal fields of any type.
                            field_info = Array.Empty<string>();
                        }
                    }

                    if (field.Contains("TU"))
                        using (var entry = field.Get("TU"))
                            if (entry is PDFString)
                            {
                                alternate_name = (entry as PDFString).Value;
                            }

                    if (field.Contains("TM"))
                        using (var entry = field.Get("TM"))
                            if (entry != null && entry is PDFString)
                            {
                                mapping_name = (entry as PDFString).Value;
                            }

                    if (field.Contains("AA"))
                        using (var entry = field.Get("AA"))
                            if (entry is PDFDict)
                            {
                                additional_actions = true;
                                var aadict = entry as PDFDict;
                                javascript_formatting = aadict.Contains("F");
                                javascript_calculation = aadict.Contains("C");
                                javascript_validation = aadict.Contains("V");
                            }

                    if (alternate_name != null)
                        Console.WriteLine("Alternate Name: " + alternate_name);
                    if (mapping_name != null)
                        Console.WriteLine("Mapping Name: " + mapping_name);
                    if (additional_actions)
                        Console.WriteLine("Additional Actions: Javascript {0}{1}{2}.",
                            javascript_validation ? "Validation, " : "",
                            javascript_calculation ? "Calculation, " : "",
                            javascript_formatting ? "Formatting" : "");

                    Console.WriteLine("Type: " + field_type);

                    if (field_flags != 0)
                    {
                        if (field_type == "Signature")
                            Console.WriteLine(String.Format("Signature Flags: {0:x8}: requires {1}{2}{3}{4}{5}{6}{7}", field_flags,
                                flags[1] ? "Filter, " : "",
                                flags[2] ? "SubFilter, " : "",
                                flags[3] ? "V, " : "",
                                flags[4] ? "Reason, " : "",
                                flags[5] ? "LegalAttestation, " : "",
                                flags[6] ? "AddRevInfo, " : "",
                                flags[7] ? "DigestMethod" : ""
                                ));
                        else
                            Console.WriteLine(String.Format("Format Flags: {0:x8}: {1}{2}{3}{4}{5}{6}{7}{8}{9}{10}{11}{12}{13}{14}{15}{16}{17}{18}", field_flags,
                                flags[1] ? "ReadOnly " : "",
                                flags[2] ? "Required " : "",
                                flags[3] ? "NoExport " : "",
                                flags[13] ? "MultiLine " : "",
                                flags[14] ? "Password " : "",
                                flags[15] ? "NoToggleToOff " : "",
                                flags[16] ? "Radio " : "",
                                flags[17] ? "PushButton " : "",
                                flags[18] ? "Combo " : "",
                                flags[19] ? "Edit " : "",
                                flags[20] ? "Sort " : "",
                                flags[21] ? "FileSelect " : "",
                                flags[22] ? "MultiSelect " : "",
                                flags[23] ? "DoNotSpellCheck " : "",
                                flags[24] ? "DoNotScroll " : "",
                                flags[25] ? "Comb " : "",
                                flags[26] ? (field_type == "Text" ? "RichText" : (field_type == "Button" ? "RadiosInUnison" : "?")) : "",
                                flags[27] ? "CommitOnSelChange " : ""
                                ));
                    }

                    foreach (string item in field_info)
                    {
                        if (item != "")
                            Console.WriteLine("\t" + item);
                    }
                    Console.WriteLine("");
                }
            }
        }

        static void DisplayRootDictionary(PDFDict formsRoot)
        {
            PDFObject entry;
            bool bNeedAppearances = false;
            int nSigFlags = 0;
            bool bCalcOrder = false;
            bool bDefaultResource = false;
            bool bDefaultAppearance = false;
            bool bXFAForms = false;
            int QuadMode = -1;
            string sQuadMode = "unkown";

            entry = formsRoot.Get("NeedAppearances");
            if (entry is PDFBoolean)
            {
                bNeedAppearances = (entry as PDFBoolean).Value;
            }

            Console.WriteLine("NeedAppearances: " + (bNeedAppearances ? "True" : "False"));

            entry = formsRoot.Get("SigFlags");
            if (entry is PDFInteger)
            {
                nSigFlags = (entry as PDFInteger).Value;
            }

            if (nSigFlags == 0)
                Console.WriteLine("Document has no signatures.");
            else
            {
                if ((nSigFlags & 1) == 1)
                    Console.WriteLine("Document has signatures.");
                if ((nSigFlags & 2) == 2)
                    Console.WriteLine("Signatures: Document may append only.");
            }

            entry = formsRoot.Get("CO");
            if (entry is PDFDict)
                bCalcOrder = true;
            Console.WriteLine(String.Format("Calculation Order Dictionary is {0}present.", (bCalcOrder ? "" : "not ")));

            entry = formsRoot.Get("DR");
            if (entry is PDFDict)
                bDefaultResource = true;
            Console.WriteLine(String.Format("Default Resource Dictionary is {0}present.", (bDefaultResource ? "" : "not ")));

            entry = formsRoot.Get("DA");
            if (entry is PDFString)
                bDefaultAppearance = true;
            Console.WriteLine(String.Format("Default Appearance String is {0}present.", (bDefaultAppearance ? "" : "not ")));

            entry = formsRoot.Get("Q");
            if (entry is PDFInteger)
            {
                QuadMode = (entry as PDFInteger).Value;
            }
            switch (QuadMode)
            {
                case -1: sQuadMode = "not present"; break;
                case 0: sQuadMode = "Left"; break;
                case 1: sQuadMode = "Centered"; break;
                case 2: sQuadMode = "Right"; break;
            }
            Console.WriteLine(String.Format("Default Quad Mode is {0}.", sQuadMode));

            entry = formsRoot.Get("XFA");
            if (entry is PDFString)
                bXFAForms = true;
            Console.WriteLine(String.Format("XFA Forms are {0}present.", (bXFAForms ? "" : "not ")));
            Console.WriteLine("");
        }

        static void DisplayPermSignatures(PDFDict docPerms)
        {
            // From section 8.7.3's TABLE 8.107 Entries in a permissions dictionary
            if (docPerms.Contains("DocMDP"))
            {
                Console.WriteLine("Document has an Author signature:");
                foreach (string item in Get_sigDict_info(docPerms.Get("DocMDP") as PDFDict))
                {
                    if (item != "")
                        Console.WriteLine(item);
                }
                Console.WriteLine("");
            }
            if (docPerms.Contains("UR3"))
            {
                Console.WriteLine("Document has a Usage Rights signature:");
                foreach (string item in Get_sigDict_info(docPerms.Get("UR3") as PDFDict))
                {
                    if (item != "")
                        Console.WriteLine(item);
                }
                Console.WriteLine("");
            }
            if (docPerms.Contains("UR"))
            {
                Console.WriteLine("Document has an old-style Usage Rights signature:");
                foreach (string item in Get_sigDict_info(docPerms.Get("UR") as PDFDict))
                {
                    if (item != "")
                        Console.WriteLine(item);
                }
                Console.WriteLine("");
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("FormWalker Sample:");

            // ReSharper disable once UnusedVariable
            using (Library lib = new())
            {
                Console.WriteLine("Initialized the library.");
                string filename = null;
                if (args.Length < 1)
                {
                    Console.Write("Enter filename: ");
                    filename = Console.ReadLine();
                }
                else
                    filename = args[0];

                using var doc = new Document(filename);
                Console.WriteLine("Opened document " + filename);
                Console.WriteLine("");

                if (doc.HasSignature)
                    Console.WriteLine("Document has a Digital Signature.");

                if (doc.Root.Contains("Perms"))
                    DisplayPermSignatures(doc.Root.Get("Perms") as PDFDict);

                PDFObject form_entry = doc.Root.Get("AcroForm");
                if (form_entry is PDFDict)
                {
                    var form_root = form_entry as PDFDict;
                    DisplayRootDictionary(form_root);

                    if (form_root.Contains("Fields"))
                        using (var fields_entry = form_root.Get("Fields"))
                            if (fields_entry is PDFArray)
                            {
                                var fields = fields_entry as PDFArray;
                                for (int i = 0; i < fields.Length; i++)
                                {
                                    using var field_entry = fields.Get(i);
                                    Enumerate_field(field_entry, "");
                                }
                            }
                }
                Console.WriteLine("Done.");
            }
        }
    }
}