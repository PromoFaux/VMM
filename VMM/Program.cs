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
            Console.WriteLine(5 / 4);

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
                    byte[] dataOld = bOld.ReadBytes(length);
                    byte[] dataNew = bNew.ReadBytes(length);
                    byte[] dataMerged = new byte[length];
                    
                    List<byte[]> tmpNew = ConvertToRows(dataNew);
                    List<byte[]> tmpOld = ConvertToRows(dataOld);
                    
                    int reqChunk = 18;

                    int xOff = (reqChunk % 16) * (17 * 16);
                    int yOff = (reqChunk / 16) * 16 ;

                    int yAt = 0;
                    for (int i = 0; i < 16; i++)
                    {

                        for (int z = 0; z < 16 * 17; z ++)
                        {
                            //chunk[z + (yAt * (16*17))] = tmpOld[yOff][xOff + z];
                            tmpOld[yOff+yAt][xOff+z]= tmpNew[yOff+yAt][xOff + z];
                        }
                        yAt += 1;
                    }

                    for (int z=0;z<256;z++)
                    {
                        Array.Copy(tmpOld[z], 0, dataMerged, z * (17 * 256), 17 * 256);
                    }

                    //THEN save "dataMerged" to file and zip up
                    
                    Console.WriteLine("chunk grabbed");

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
                    
                    bOld.Dispose();
                    bNew.Dispose();

                    File.Delete(extract + "\\datanew");
                    File.Delete(extract + "\\dataold");

                    //if (somethingChanged)
                    //{
                    //    Console.WriteLine("Changes Made: " + f.Name);
                    //    File.WriteAllBytes(extract + "\\data", dataMerged);
                    //    File.Delete(dirMer + "\\" + f.Name);
                    //    ZipFile.CreateFromDirectory(extract, dirMer + "\\" + f.Name);
                    //    File.Delete(extract + "\\data");
                    //}

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

        public static byte[] Combine(params byte[][] arrays)
        {
            byte[] ret = new byte[arrays.Sum(x => x.Length)];
            int offset = 0;
            foreach (byte[] data in arrays)
            {
                Buffer.BlockCopy(data, 0, ret, offset, data.Length);
                offset += data.Length;
            }
            return ret;
        }

        public static List<byte[]> ConvertToRows(byte[] inArray)
        {
            List<byte[]> outListArray = new List<Byte[]>();

            for(int x=0; x < inArray.Length; x+=17*256)
            {
                byte[] tmp = new byte[17 * 256];
                for (int z = 0; z < 17 * 256; z++) 
                {
                    tmp[z] = inArray[z + x];
                }
                
                outListArray.Add(tmp);
            }

            return outListArray;
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


