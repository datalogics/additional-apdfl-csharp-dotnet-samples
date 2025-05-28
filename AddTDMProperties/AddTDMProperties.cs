using Datalogics.PDFL;

/*
 * This program adds TDM-aware properties to a PDF's XMP metadata,
 * in accordance with https://www.w3.org/community/reports/tdmrep/CG-FINAL-tdmrep-20240510
 * 
 * Copyright (c) 2007-2025, Datalogics, Inc. All rights reserved.
 *
 */
namespace AddTDMProperties
{

    class AddTDMProperties
    {

        static void Main(string[] args)
        {
            Console.WriteLine("TDMRep Sample:");

            using (Library lib = new Library())
            {
                Console.WriteLine("Initialized the library.");

                String sInput = Library.ResourceDirectory + "Sample_Input/pdf_intro.pdf";
                String sOutput = Library.ResourceDirectory + "Sample_Input/TDMRep-out.pdf";
                String policyURI = Library.ResourceDirectory + "https://www.datalogics.com/tdm_policy_goes_here";

                if (args.Length > 0)
                    sInput = args[0];

                Document doc = new Document(sInput);
                Console.WriteLine("Input file:  " + sInput);

                const string tdmNS = "http://www.w3.org/ns/tdmrep";

                doc.SetXMPMetadataProperty(tdmNS, "tdm", "reservation", "1");
                doc.SetXMPMetadataProperty(tdmNS, "tdm", "policy", policyURI);

                doc.Save(SaveFlags.Full, sOutput);
            }
        }
    }
}
