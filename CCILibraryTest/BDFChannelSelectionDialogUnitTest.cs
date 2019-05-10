using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BDFChannelSelection;
using BDFEDFFileStream;
using ElectrodeFileStream;

namespace CCILibraryTest
{
    [TestClass]
    public class ChannelSelectionDialogTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            BDFEDFFileReader bdf = new BDFEDFFileReader(new FileStream("../../Test files/HeaderOnly.bdf", FileMode.Open, FileAccess.Read));
            ElectrodeInputFileStream etr = new ElectrodeInputFileStream(new FileStream("../../Test files/HeaderOnly.etr", FileMode.Open, FileAccess.Read));
            BDFChannelSelectionDialog dialog = new BDFChannelSelectionDialog(bdf, etr);
            bool ret = (bool)dialog.ShowDialog();
            if (ret)
            {
                foreach (ChannelDescription cd in dialog.SelectedChannels)
                {
                    if (cd.Selected)
                        Console.WriteLine("Name = {0}; Type = {1}; EEG = {2}", cd.Name, cd.Type, cd.EEG);
                }
            }
            else
            {
                Console.WriteLine("Canceled");
            }
            while (ret)
            {
                Console.WriteLine();
                dialog = new BDFChannelSelectionDialog(dialog.SelectedChannels, etr);
                ret = (bool)dialog.ShowDialog();
                Console.WriteLine(ret ? "Updated" : "Unchanged");
                foreach (ChannelDescription cd in dialog.SelectedChannels)
                {
                    if (cd.Selected)
                        Console.WriteLine("Name = {0}; Type = {1}; EEG = {2}", cd.Name, cd.Type, cd.EEG);
                }
            }
        }

        [TestMethod]
        public void TestMethod2()
        {
            BDFEDFFileReader bdf = new BDFEDFFileReader(new FileStream("../../Test files/HeaderOnly.bdf", FileMode.Open, FileAccess.Read));
            BDFChannelSelectionDialog dialog = new BDFChannelSelectionDialog(bdf);
            bool? ret = dialog.ShowDialog();
            if ((bool)ret)
            {
                foreach (ChannelDescription cd in dialog.SelectedChannels)
                {
                    if (cd.Selected)
                        Console.WriteLine("Name = {0}; Type = {1}; EEG = {2}", cd.Name, cd.Type, cd.EEG);
                }
            }
        }
    }
}
