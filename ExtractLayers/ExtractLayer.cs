
using Datalogics.PDFL;

/*
 *  This sample demonstrates how to extract the individual layers from a layered document.
 *
 *
 * The results are exported to several PDF output documents.
 * 
 *
 * Copyright (c) 2007-2024, Datalogics, Inc. All rights reserved.
 *
 */
namespace ExtractLayers
{
    class ExtractLayers
    {
        private static HashSet<String> FindLayerNames(OptionalContentOrderArray ocoa)
        {
            HashSet<string> layerNames = new();
            if (ocoa == null)
                return layerNames;

            int arraylen = ocoa.Length;
            for (int i = 0; i < arraylen; i++)
            {
                using OptionalContentOrderNode node = ocoa.Get(i);
                if (node == null)
                    continue;

                if (node is OptionalContentOrderArray)
                    layerNames.UnionWith(FindLayerNames(node as OptionalContentOrderArray));
                else
                {
                    layerNames.Add((node as OptionalContentOrderLeaf).OptionalContentGroup.Name);
                }
            }

            return layerNames;
        }


        static void Main(string[] args)
        {
            Console.WriteLine("ExtractLayers Sample:");

            // ReSharper disable once UnusedVariable
            using (Library lib = new())
            {
                Console.WriteLine("Initialized the library.");

                String sInput = Library.ResourceDirectory + "Sample_Input/layers.pdf";

                if (args.Length > 0)
                    sInput = args[0];

                Console.WriteLine("Input file: " + sInput);

                HashSet<String> layerNames = new();

                //Step 1. Get the Layer Names from the Optional Content Configuration(s)
                using (var doc = new Document(sInput))
                {
                    foreach (OptionalContentConfig cfg in doc.OptionalContentConfigs)
                    {
                        if(cfg != null && cfg.Order != null)
                            layerNames.UnionWith(FindLayerNames(cfg.Order));
                    }
                }

                //step 2. for every layer name we found, extract from every page all Forms and Containers associated with this layer name.
                //NOTE: This assumes that all Optional Content Groups are at the top-level of the page's rather than nested within other Forms and Containers
                // this assumption is not likely to hold with all real-world documents.
                //NOTE: Annotations can also be part of Optional Content groups, but this version does not attempt to remove any annotations that are not
                //part of the current layer.
                foreach (String layer in layerNames)
                {
                    using var doc = new Document(sInput);
                    for (int i = 0; i < doc.NumPages; i++)
                    {
                        using var pg = doc.GetPage(i);
                        using var pgContent = pg.Content;
                        for (int j = pgContent.NumElements - 1; j > -1; j--)
                        {
                            var foundCurLayer = false;
                            var elem = pgContent.GetElement(j);
                            if (elem is Form)
                            {
                                var md = (elem as Form).OptionalContentMembershipDict;
                                if (md != null)
                                {
                                    foreach (var ocg in md.OptionalContentGroups)
                                    {
                                        if (ocg.Name.Equals(layer))
                                            foundCurLayer = true;
                                    }
                                }
                                if (!foundCurLayer)
                                    pgContent.RemoveElement(j);
                            }
                            else if (elem is Container)
                            {
                                var md = (elem as Container).OptionalContentMembershipDict;
                                if (md != null)
                                {
                                    foreach (var ocg in md.OptionalContentGroups)
                                    {
                                        if (ocg.Name.Equals(layer))
                                            foundCurLayer = true;
                                    }
                                }
                                if (!foundCurLayer)
                                    pgContent.RemoveElement(j);
                            }
                            else
                            {
                                pgContent.RemoveElement(j);
                            }
                        }
                        pg.UpdateContent();
                    }
                    doc.Save(SaveFlags.Full, sInput.Replace(".pdf", "_" + layer + ".pdf"));                  
                    doc.Close();
                }
            }
        }
    }
}

