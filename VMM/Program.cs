using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace VMM
{
    class Program
    {
        static void Main(string[] args)
        {
            string newDir = @"E:\voxel\new";
            string curDir = @"E:\voxel\current";
            string merDir = @"E:\voxel\merged";
            string extract = @"E:\voxel\tmp";
            DateTime newMd, oldMd;



            byte[] empty = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            DirectoryInfo dirNew = new DirectoryInfo(newDir);
            DirectoryInfo dirCur = new DirectoryInfo(curDir);
            DirectoryInfo dirMer = new DirectoryInfo(merDir);


            //Copy out the current files to the merged directory (in case something goes wrong!)
            foreach (FileInfo f in dirCur.GetFiles())
            {
                f.CopyTo(merDir + "\\" + f.Name);
            }

            //Go through the files in the new directory and merge them into the merge directory if needed
            foreach (FileInfo f in dirNew.GetFiles())
            {
                FileInfo[] fN = dirMer.GetFiles(f.Name);
                if (fN.Length > 0)
                {

                    //Match found in "merged" directory!
                    //extract and merge (don't worry about dates until we're comparing packets)
                    newMd = f.LastWriteTime;
                    oldMd = fN[0].LastWriteTime;
                    ZipFile.ExtractToDirectory(f.FullName, extract);
                    File.Copy(extract + "\\data", extract + "\\datanew");
                    File.Delete(extract + "\\data");

                    ZipFile.ExtractToDirectory(fN[0].FullName, extract);
                    File.Copy(extract + "\\data", extract + "\\dataold");
                    File.Delete(extract + "\\data");

                    //open dataold and datanew for binary analysis
                    BinaryReader bOld = new BinaryReader(File.Open(extract + "\\dataold", FileMode.Open));
                    BinaryReader bNew = new BinaryReader(File.Open(extract + "\\datanew", FileMode.Open));

                    int pos = 0;
                    int length = (int)bOld.BaseStream.Length;


                    var coord1 = new byte[17];
                    var coord2 = new byte[17];
                    byte[] dataOld = bOld.ReadBytes(length);
                    byte[] dataNew = bNew.ReadBytes(length);
                    byte[] dataMerged = new byte[dataNew.Length];
                    bool somethingChanged = false;
                    //this will read block by block
                    //for (int i = 0; i < dataOld.Length; i += 17)
                    //{
                    //    for (int x = 0; x < 17; x++)
                    //    {
                    //        coord1[x] = dataNew[i + x];
                    //        coord2[x] = dataOld[i + x];                           
                    //    }

                    //    if (!coord1.SequenceEqual(coord2) && !(coord1.SequenceEqual(empty)))
                    //    {
                    //        if (newMd > oldMd)
                    //        {
                    //            coord1.CopyTo(dataMerged, i);
                    //            somethingChanged = true;
                    //        }
                    //        else
                    //        {
                    //            coord2.CopyTo(dataMerged, i);
                    //        }
                    //    }
                    //    else
                    //    {
                    //        coord2.CopyTo(dataMerged, i);
                    //    }                      
                    //}

                    //lets read chunk by chunk
                    //256 chunks in data file
                    var chunk1 = new byte[256 * 17];
                    var chunk2 = new byte[256 * 17];
                    for (int c = 0; c < 256; c++)
                    {
                        //y
                        for (int cy = 0; cy < 16; cy++)
                        {
                            //x
                            for (int cx = 0; cx < 16; cx++)
                            {
                                //get block at that xy
                                for (int x = 0; x < 17; x++)
                                {
                                    int btr = (c * (256*17)) + (cy * (17*16)) + (cx * 17);
                                    int btw = cy * (16*17) + x; 
                                   
                                    chunk1[btw] = dataNew[btr];
                                    chunk2[btw] = dataOld[btr];
                                }
                               
                            }
                        }
                        //should now have a chunk
                        if (!chunk1.SequenceEqual(chunk2))
                        {
                            Console.WriteLine("MISMATCHED CHUNK");
                        }


                    }


                    bOld.Dispose();
                    bNew.Dispose();

                    File.Delete(extract + "\\datanew");
                    File.Delete(extract + "\\dataold");

                    if (somethingChanged)
                    {
                        Console.WriteLine("Changes Made: " + f.Name);
                        File.WriteAllBytes(extract + "\\data", dataMerged);
                        File.Delete(dirMer + "\\" + f.Name);
                        ZipFile.CreateFromDirectory(extract, dirMer + "\\" + f.Name);
                        File.Delete(extract + "\\data");
                    }

                }
                else
                {
                    Console.WriteLine("No Changes Needed: " + f.Name);
                    //No changes to make, copy to merged directory without changing anything
                    f.CopyTo(merDir + "\\" + f.Name);
                }
            }

            //foreach (FileInfo fM in dirMer.GetFiles())
            //{
            //    File.Delete(fM.FullName);
            //}
        }


        public static void PrintByteArray(byte[] bytes)
        {
            var sb = new StringBuilder("new byte[] { ");
            foreach (var b in bytes)
            {
                sb.Append(b + ", ");
            }
            sb.Append("}");
            Console.WriteLine(sb.ToString());
        }


    }

}
