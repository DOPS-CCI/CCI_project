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
        public void ChannelSelectionDialogTest1()
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
                ChannelSelection sc = dialog.SelectedChannels;
                foreach (ChannelDescription cd in sc)
                {
                    if (cd.Selected)
                        Console.WriteLine("Name = {0}; Type = {1}; EEG = {2}", cd.Name, cd.Type, cd.EEG);
                }
                Console.Write("Total: B={0}, AE={1}, EEG={2}, NonAE={3}", sc.BDFTotal, sc.AETotal, sc.EEGTotal, sc.NonAETotal);
                Console.Write("Selected: B={0}, AE={1}, EEG={2}, NonAE={3}", sc.BDFSelected, sc.AESelected, sc.EEGSelected, sc.NonAESelected);
            }
        }

        [TestMethod]
        public void ChannelSelectionDialogTest2()
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
