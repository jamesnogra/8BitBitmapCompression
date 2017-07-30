using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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

        public Form1()
        {
            InitializeComponent();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            mainImage = new Bitmap(openFileDialog1.FileName);
            pictureBox.Image = mainImage;
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Made by James Arnold E. Nogra");
        }

        private void compressBtn_Click(object sender, EventArgs e)
        {
            allColors = new List<ColorData>();
            Color tempColor;

            //iterate to the whole image (widthxheight)
            for (int x=0; x<mainImage.Width; x++)
            {
                for (int y=0; y<mainImage.Height; y++)
                {
                    tempColor = mainImage.GetPixel(x, y);
                    addColorOrIncreaseColorCount(tempColor.R);
                    addColorOrIncreaseColorCount(tempColor.G);
                    addColorOrIncreaseColorCount(tempColor.B);
                }
            }
            allColors = allColors.OrderBy(o => o.count).ToList();
            createHuffmanTree();
            //MessageBox.Show(printAllIAllColors());
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
            MessageBox.Show(printAllHuffmanCodes());
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
            traverseHuffmanTreeForDictionary(theTree.left, ("0" + currentPrefixCode));
            traverseHuffmanTreeForDictionary(theTree.right, ("1" + currentPrefixCode));
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
