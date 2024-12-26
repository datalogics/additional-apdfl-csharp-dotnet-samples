using System;
using System.Collections.Generic;
using System.Text;
using Datalogics.PDFL;

/*
 * The rotateText sample program creates a new PDF file with a page per system font that can represent a given string of text.
 * 
 * Copyright (c) 2007-2024, Datalogics, Inc. All rights reserved.
 *
 */

namespace rotateText
{
    class RotateText
    {
        const double radianFactor = Math.PI / 180;
        static void Main(string[] args)
        {
            Console.WriteLine("RotateText Sample:");

            using (Library lib = new Library())
            {
                Console.WriteLine("Initialized the library.");

                String sOutput = "rotateText-out.pdf";
                if (args.Length > 0)
                    sOutput = args[0];
                Console.WriteLine("Output file: " + sOutput);

                Document doc = new Document();

                var pageRect = new Rect(0, 0, 1684, 1191); //A2 paper size
                //String testString = "The quick brown fox jumped over the lazy dog.";
                String testString = "Kæmi ný öxi hér, ykist þjófum nú bæði víl og ádrepa."; // icelandic pangram; whittle's down the size of the output file a bit.
                double angle = 20.0;
                double fontSize = 20.8999938964;
                double radius = fontSize / Math.Sin(angle * radianFactor);
                
                var fillColor = new Color(1.0, 1.0, 0); // Yellow
                var strokeColor = new Color(153.0 / 255.0, 0, 0); // kind of a deep red  
                var textColor = new Color(0, 0, 1.0);

                foreach (Font sysFont in Font.FontList)
                {
                    using (var f = new Font(sysFont.Name, FontCreateFlags.Embedded | FontCreateFlags.Subset))
                    {
                        if (f.IsTextRepresentable(testString))
                        {
                            using (var docpage = doc.CreatePage(Document.LastPage, pageRect))
                            {
                                var center = new Point(pageRect.Left + (0.5 * (pageRect.Right - pageRect.Left)), pageRect.Top - (0.5 * (pageRect.Top - pageRect.Bottom)));

                                var polygon = new Path()
                                {
                                    PaintOp = PathPaintOpFlags.EoFill | PathPaintOpFlags.Stroke,
                                    GraphicState = new GraphicState()
                                    {
                                        FillColor = fillColor,
                                        StrokeColor = strokeColor,                            
                                        LineJoin = LineJoin.BevelJoin,
                                        Width = 1.0
                                    }
                                };

                                polygon.MoveTo(new Point(center.H+radius, center.V));

                                var t = new Text();
                                var gs = new GraphicState() { FillColor = textColor };
                                var ts = new TextState() { FontSize = fontSize };

                                for (double thetaD = 0; thetaD < 360; thetaD +=angle)
                                {
                                    double theta1 = thetaD * radianFactor;
                                    double x1 = center.H + radius * Math.Cos(theta1);
                                    double y1 = center.V + radius * Math.Sin(theta1);

                                    polygon.AddLine(new Point(x1, y1));

                                    using (Matrix m = new Matrix().Translate(x1, y1).Concat(new Matrix().Rotate(thetaD)))
                                    using (TextRun tr = new TextRun(testString, f, gs, ts, m))
                                        t.AddRun(tr);
                                }
                                polygon.ClosePath();
                                docpage.Content.AddElement(polygon); 

                                docpage.Content.AddElement(t);
                                docpage.UpdateContent();
                            }
                        }
                    }
                }

                doc.EmbedFonts(EmbedFlags.None);
                doc.Save(SaveFlags.Full | SaveFlags.AddFlate | SaveFlags.Compressed, sOutput);
            }
        }
    }
}
