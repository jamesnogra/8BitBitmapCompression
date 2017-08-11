using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _8BitBitmapCompression
{
    public partial class Form1 : Form
    {
        Bitmap mainImage;
        List<ColorData> allColors;
        List<HuffmanDictionary> allPrefixCodes;
        SaveFileDialog saveFile;
        float originalFileSize;
        DateTime start, end;
        byte[] arr;

        public Form1()
        {
            InitializeComponent();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1 .Filter = "Bitmap files (*.bmp)|*.bmp|All files (*.*)|*.*";
            openFileDialog1.ShowDialog();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            mainImage = new Bitmap(openFileDialog1.FileName);
            var fileInfo = new FileInfo(openFileDialog1.FileName);
            originalFileSize = fileInfo.Length;
            pictureBox.Image = mainImage;
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Made by James Arnold E. Nogra");
        }

        private void compressBtn_Click(object sender, EventArgs e)
        {
            try
            {
                //save to file
                saveFile = new SaveFileDialog();
                saveFile.FileName = "Compressed.8bt";
                saveFile.Filter = "Text files (*.8bt)|*.8bt|All files (*.*)|*.*";
                if (saveFile.ShowDialog() == DialogResult.OK)
                {
                    start = DateTime.UtcNow;
                    allColors = new List<ColorData>();
                    Color tempColor;

                    //iterate to the whole image (widthxheight)
                    for (int x = 0; x < mainImage.Width; x++)
                    {
                        for (int y = 0; y < mainImage.Height; y++)
                        {
                            tempColor = mainImage.GetPixel(x, y);
                            addColorOrIncreaseColorCount(tempColor.R);
                            addColorOrIncreaseColorCount(tempColor.G);
                            addColorOrIncreaseColorCount(tempColor.B);
                        }
                    }
                    allColors = allColors.OrderBy(o => o.count).ToList();
                    createHuffmanTree();
                    createCompressedFile();
                    end = DateTime.UtcNow;
                    var lapseTime = Math.Abs((end - start).TotalSeconds);
                    resultOriginalFileSize.Text = Convert.ToString(originalFileSize);
                    resultCompressedFileSize.Text = Convert.ToString(arr.Length);
                    resultDuration.Text = Convert.ToString(lapseTime);
                }
                    
            }
            catch (Exception err)
            {
                MessageBox.Show("Error in compressing the image. Maybe there's no image set.");
            }
        }

        private void createHuffmanTree()
        {
            List<HuffmanTreeData> theTree = new List<HuffmanTreeData>();
            HuffmanTreeData tempData, tempLeft, tempRight;

            //FIRST put all the items in the allColors to the new list
            foreach (ColorData e in allColors)
            {
                tempData = new HuffmanTreeData(e.colorCode, e.count, null, null, true);
                theTree.Add(tempData);
            }

            if (allColors.Count>1)
            {
                //make the three
                //extract the two items with the least count and make a new node
                while (theTree.Count>1)
                {
                    //sort the treeData so that we can extract the least two
                    theTree = theTree.OrderBy(o => o.totalCount).ToList();
                    tempLeft = theTree.First(); //left of the new tree
                    theTree.RemoveAt(0);
                    tempRight = theTree.First(); //right of the new tree
                    theTree.RemoveAt(0);
                    tempData = new HuffmanTreeData((tempLeft.totalCount+tempRight.totalCount), tempLeft, tempRight, false);
                    theTree.Add(tempData);
                    //MessageBox.Show("Making new node with total count " + (tempLeft.totalCount + tempRight.totalCount) + "\nLeft Total: " + tempLeft.totalCount + "\nLeft Total: " + tempRight.totalCount);
                }

                //traverse the tree which is at the first element of the theTree
                allPrefixCodes = new List<HuffmanDictionary>();
                traverseHuffmanTreeForDictionary(theTree.First(), "");
            }
            else //what if there is only one byte data, then the prefix code for that one item is '0'
            {
                allPrefixCodes = new List<HuffmanDictionary>();
                HuffmanDictionary oneItem;
                tempData = theTree.First();
                oneItem = new HuffmanDictionary(tempData.item, "0", tempData.totalCount);
                allPrefixCodes.Add(oneItem);
            }
            allPrefixCodes = allPrefixCodes.OrderByDescending(o => o.totalCount).ToList();
            //MessageBox.Show(printAllHuffmanCodes());
        }

        //this will be called recursively, only stops when reaches the leaves
        public void traverseHuffmanTreeForDictionary(HuffmanTreeData theTree, string currentPrefixCode)
        {
            HuffmanDictionary tempData;
            if (theTree.isLeaf) //if this is the leaf, then we add to the list of Dictionary
            {
                tempData = new HuffmanDictionary(theTree.item, currentPrefixCode, theTree.totalCount);
                allPrefixCodes.Add(tempData);
                return;
            }
            traverseHuffmanTreeForDictionary(theTree.left, (currentPrefixCode + "0"));
            traverseHuffmanTreeForDictionary(theTree.right, (currentPrefixCode + "1"));
        }

        private void createCompressedFile()
        {
            Color tempColor;
            string tempStr = "";
            tempStr += Convert.ToString(mainImage.Width, 2).PadLeft(16, '0'); //16 bit for width
            tempStr += Convert.ToString(mainImage.Height, 2).PadLeft(16, '0'); //16 bit for height
            tempStr += Convert.ToString(allPrefixCodes.Count, 2).PadLeft(8, '0'); //8 bit for size of dictionary
            
            //iterate through the prefix codes for dictionary
            foreach (HuffmanDictionary i in allPrefixCodes)
            {
                tempStr += Convert.ToString(i.item, 2).PadLeft(8, '0'); //8 bit for color code
                tempStr += Convert.ToString(i.prefixCode.Length, 2).PadLeft(4, '0'); //4 bit for the length of the prefix code
                tempStr += i.prefixCode; //append the prefix code
                //MessageBox.Show("Color Code: " + Convert.ToString(i.item, 2).PadLeft(8, '0') + "\nPrefix Code Length: " + Convert.ToString(i.prefixCode.Length, 2).PadLeft(4, '0') + "\nPrefix Code: " + i.prefixCode);
            }

            //convert the byte data of each pixel in the image to the corresponding prefix codes
            for (int x = 0; x < mainImage.Width; x++)
            {
                for (int y = 0; y < mainImage.Height; y++)
                {
                    tempColor = mainImage.GetPixel(x, y);
                    tempStr += getPrefixCodeOfColorData(tempColor.R);
                    tempStr += getPrefixCodeOfColorData(tempColor.G);
                    tempStr += getPrefixCodeOfColorData(tempColor.B);
                }
            }
            Stream stream = new FileStream(saveFile.FileName, FileMode.Create);
            BinaryWriter bw = new BinaryWriter(stream);
            arr = StringToBytesArray(tempStr);
            bw.Write(arr);
            bw.Flush();
            bw.Close();
        }

        private string getPrefixCodeOfColorData(byte item)
        {
            foreach (HuffmanDictionary i in allPrefixCodes)
            {
                if (i.item == item)
                {
                    return i.prefixCode;
                }
            }
            return "";
        }

        private void addColorOrIncreaseColorCount(Byte singleColor)
        {
            bool isAlreadyInList = false;
            foreach (ColorData eachColor in allColors)
            {
                if (singleColor == eachColor.colorCode)
                {
                    eachColor.count++;
                    isAlreadyInList = true;
                    break;
                }
            }
            if (!isAlreadyInList)
            {
                ColorData newColorData = new ColorData(singleColor);
                allColors.Add(newColorData);
            }
        }

        private string printAllIAllColors()
        {
            string tempString = "Total Unique Colors: " + allColors.Count + "\n";
            foreach (ColorData eachColor in allColors)
            {
                tempString += eachColor + "\n";
            }
            return tempString;
        }

        public string printAllHuffmanCodes()
        {
            string tempString = "All Huffman Codes\n\n";
            foreach (HuffmanDictionary e in allPrefixCodes)
            {
                tempString += e + "\n";
            }
            return tempString;
        }

        //from https://stackoverflow.com/questions/41778077/how-to-write-a-string-of-binary-to-file-c-sharp
        private byte[] StringToBytesArray(string str)
        {
            var bitsToPad = 8 - str.Length % 8;
            if (bitsToPad != 8)
            {
                var neededLength = bitsToPad + str.Length;
                str = str.PadRight(neededLength, '0');
            }
            Clipboard.SetText(str);
            int size = str.Length / 8;
            byte[] arr = new byte[size];
            for (int a = 0; a < size; a++)
            {
                arr[a] = Convert.ToByte(str.Substring(a * 8, 8), 2);
                //MessageBox.Show("At byte " + a + " is " + arr[a]);
            }
            return arr;
        }

        private void extractBtn_Click(object sender, EventArgs e)
        {
            openFileForExtract.Filter = "8-bit Compressed Bitmap Files (*.8bt)|*.8bt|All files (*.*)|*.*";
            openFileForExtract.FileName = "Compressed.8bt";
            openFileForExtract.ShowDialog();
        }

        private void openFileForExtract_FileOk(object sender, CancelEventArgs e)
        {
            string readText, binaryFileStr = "";
            try
            {
                start = DateTime.UtcNow;
                byte[] allBytes = System.IO.File.ReadAllBytes(openFileForExtract.FileName);
                readText = File.ReadAllText(openFileForExtract.FileName);
                foreach (byte c in allBytes)
                {
                    //MessageBox.Show("Byte is " + c);
                    binaryFileStr += Convert.ToString(c, 2).PadLeft(8, '0');
                }
                processExtractedFile(binaryFileStr);
                end = DateTime.UtcNow;
                var lapseTime = Math.Abs((end - start).TotalSeconds);
                resultDuration.Text = Convert.ToString(lapseTime);
            }
            catch (IOException)
            {
                MessageBox.Show("Something went wrong in reading the file;");
            }
        }

        private void processExtractedFile(string binaryFileStr)
        {
            int width, height;
            byte dictionarySize, colorData, prefixCodeLength;
            string prefixCode, tempPrefixCodeLengthBinary, tempItemBinary;
            List<HuffmanDictionary> extractedPrefixCodes = new List<HuffmanDictionary>();
            Bitmap extractedImage;

            //process first the first four bytes for width and height
            width = Convert.ToInt32(binaryFileStr.Substring(0, 16), 2);
            binaryFileStr = binaryFileStr.Remove(0, 16);
            height = Convert.ToInt32(binaryFileStr.Substring(0, 16), 2);
            binaryFileStr = binaryFileStr.Remove(0, 16);

            //8 bit for the dictionary size
            dictionarySize = Convert.ToByte(binaryFileStr.Substring(0, 8), 2);
            binaryFileStr = binaryFileStr.Remove(0, 8);
            //MessageBox.Show("Dimensions: " + width + "x" + height + "\nDictionary Size: " + dictionarySize);

            //iterate through the dictionary using the dictionarySize
            for (int x=0; x<dictionarySize; x++)
            {
                //get first the color data (8 bits)
                colorData = Convert.ToByte(binaryFileStr.Substring(0, 8), 2);
                tempItemBinary = binaryFileStr.Substring(0, 8);
                binaryFileStr = binaryFileStr.Remove(0, 8);
                //4 bit for the length of the prefix code
                prefixCodeLength = Convert.ToByte(binaryFileStr.Substring(0, 4), 2);
                tempPrefixCodeLengthBinary = binaryFileStr.Substring(0, 4);
                binaryFileStr = binaryFileStr.Remove(0, 4);
                //then get the prefix code based on the prefixCodeLength
                prefixCode = binaryFileStr.Substring(0, prefixCodeLength);
                binaryFileStr = binaryFileStr.Remove(0, prefixCodeLength);
                extractedPrefixCodes.Add(new HuffmanDictionary(colorData, prefixCode, 0));
                //MessageBox.Show("Color Data: " + colorData + " From ("+ tempItemBinary + ")" + "\nPrefix Code Length: " + prefixCodeLength + " (From "+ tempPrefixCodeLengthBinary + ") " + "\nPrefix Code: " + prefixCode);
            }

            //put the pixels of the image back
            bool prefixCodeFound;
            byte eRed, eGreen, eBlue;
            byte[] rgbColors = new byte[3];
            int byteFound;
            extractedImage = new Bitmap(width, height);
            for (int x=0; x<width; x++)
            {
                for (int y=0; y<height; y++)
                {
                    for (int z=0; z<3; z++) //this is for RGB
                    {
                        //this is for RED
                        prefixCodeFound = false;
                        prefixCodeLength = 1;
                        while (!prefixCodeFound)
                        {
                            prefixCode = binaryFileStr.Substring(0, prefixCodeLength);
                            byteFound = matchPrefixCodeWithColorData(extractedPrefixCodes, prefixCode);
                            if (byteFound != -1)
                            {
                                rgbColors[z] = (byte)byteFound;
                                //MessageBox.Show("Found " + eRed);
                                prefixCodeFound = true;
                                binaryFileStr = binaryFileStr.Remove(0, prefixCodeLength);
                            }
                            else
                            {
                                prefixCodeLength++;
                            }
                        }
                    }
                    extractedImage.SetPixel(x, y, Color.FromArgb(rgbColors[0], rgbColors[1], rgbColors[2]));
                    //MessageBox.Show("Colors: ("+ rgbColors[0] + "," + rgbColors[1] + "," + rgbColors[2] + ")");
                }
            }
            extractedPicBox.Image = extractedImage;
        }

        private int matchPrefixCodeWithColorData(List<HuffmanDictionary> tempList, string prefixCode)
        {
            foreach (HuffmanDictionary a in tempList)
            {
                if (a.prefixCode == prefixCode)
                {
                    return a.item;
                }
            }
            return -1;
        }
    }

    public class HuffmanTreeData
    {
        public byte item;
        public int totalCount;
        public HuffmanTreeData left;
        public HuffmanTreeData right;
        public bool isLeaf;
        public HuffmanTreeData(byte item, int totalCount, HuffmanTreeData left, HuffmanTreeData right, bool isLeaf)
        {
            this.item = item;
            this.totalCount = totalCount;
            this.left = left;
            this.right = right;
            this.isLeaf = isLeaf;
        }
        public HuffmanTreeData(int totalCount, HuffmanTreeData left, HuffmanTreeData right, bool isLeaf)
        {
            this.totalCount = totalCount;
            this.left = left;
            this.right = right;
            this.isLeaf = isLeaf;
        }
    }

    public class HuffmanDictionary
    {
        public byte item;
        public string prefixCode;
        public int totalCount;
        public HuffmanDictionary(byte item, string prefixCode, int totalCount)
        {
            this.item = item;
            this.prefixCode = prefixCode;
            this.totalCount = totalCount;
        }
        public override string ToString()
        {
            return "Item: " + this.item + "\t\tTotal Count: " + this.totalCount + "\t\tPrefix Code: " + this.prefixCode;
        }
    }

    public class ColorData
    {
        public byte colorCode;
        public int count;
        public ColorData(byte colorCode)
        {
            this.colorCode = colorCode;
            this.count = 1;
        }
        public override string ToString()
        {
            return "Color: " + this.colorCode + "\t\tCount: " + this.count;
        }
    }
}
