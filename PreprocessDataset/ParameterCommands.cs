using System;
using System.IO;
using System.Windows.Input;
using System.Windows.Forms;
using System.Xml;
using CCIUtilities;

namespace PreprocessDataset
{
    public partial class MainWindow
    {
        public static RoutedUICommand OpenPCommand = new RoutedUICommand("OpenP", "OpenP", typeof(MainWindow));
        public static RoutedUICommand SavePCommand = new RoutedUICommand("SaveP", "SaveP", typeof(MainWindow));
        public static RoutedUICommand ProcessCommand = new RoutedUICommand("Process", "Process", typeof(MainWindow));
        public static RoutedUICommand ExitCommand = new RoutedUICommand("Exit", "Exit", typeof(MainWindow));

        private void InitializeMenuBindings()
        {
            //***** Set up menu commands and short cuts

            CommandBinding cbOpenP = new CommandBinding(OpenPCommand, cbOpen_Execute, cbOpen_CanExecute);
            this.CommandBindings.Add(cbOpenP);

            CommandBinding cbSaveP = new CommandBinding(SavePCommand, cbSave_Execute, validParams_CanExecute);
            this.CommandBindings.Add(cbSaveP);

            CommandBinding cbProcess = new CommandBinding(ProcessCommand, Process_Click, validParams_CanExecute);
            this.CommandBindings.Add(cbProcess);

            CommandBinding cbExit = new CommandBinding(ExitCommand, Quit_Click, cbExit_CanExecute);
            this.CommandBindings.Add(cbExit);
        }

        private void cbOpen_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            PerformOpenPFile();
        }

        private void cbOpen_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void cbSave_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            PerformSavePFile();
        }

        private void validParams_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ProcessButton.IsEnabled; //only permit when processing can execute
        }

        private void cbExit_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = QuitButton.IsEnabled;
        }

        private void PerformSavePFile()
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Title = "Save parameter file ...";
            dlg.DefaultExt = ".par"; // Default file extension
            dlg.Filter = "PAR Files (.par)|*.par"; // Filter files by extension
            dlg.InitialDirectory = Properties.Settings.Default.LastParFile;
            bool result = dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK;
            if (!result) return;

            Properties.Settings.Default.LastParFile = System.IO.Path.GetDirectoryName(dlg.FileName);

            XmlWriterSettings xws = new XmlWriterSettings();
            xws.Indent = true;
            xws.CloseOutput = true;
            XmlWriter xml = XmlWriter.Create(new FileStream(dlg.FileName, FileMode.Create, FileAccess.Write), xws);
            xml.WriteStartDocument();

            xml.WriteStartElement("PreprocessorParameters");
            xml.WriteAttributeString("Type",
                ppw.inputType == InputType.RWNL ? "RWNL" : ppw.inputType == InputType.BDF ? "BDF" : "SET");

            xml.WriteElementString("InputDecimation", ppw.SR.Decimation1.ToString("0"));
            if (ppw.doDetrend)
                xml.WriteElementString("Detrend", ppw.detrendOrder.ToString("0"));

            if (ppw.doReference)
            {
                xml.WriteStartElement("Reference");
                xml.WriteAttributeString("ExcludeUnselected",ppw._refExcludeElim?"True":"False");
                if (ppw._refType == 1)
                {
                    xml.WriteAttributeString("Type", "Channels");
                    xml.WriteElementString("RefChannels", Utilities.intListToString(ppw._refChan, true));
                }
                else if (ppw._refType == 2)
                {
                    xml.WriteAttributeString("Type", "Expression");
                    xml.WriteElementString("RefExpression", RefChanExpression.Text);
                }
                else
                {
                    xml.WriteAttributeString("Type", "Matrix");
                    xml.WriteElementString("RefMatrixFile", RefMatrixFile.Text);
                }
                xml.WriteEndElement(/*Reference*/);
            }

            if (ppw.doFiltering)
            {
                xml.WriteStartElement("Filters");
                xml.WriteAttributeString("Causal", ppw.reverse ? "False" : "True");

                foreach (IFilterDesignControl df in FilterList.Items)
                {
                    Tuple<string, int, double[]> t = df.Filter.Description;
                    xml.WriteStartElement("IIRFilter");
                    xml.WriteAttributeString("Type", t.Item1);
                    xml.WriteElementString("Poles", t.Item2.ToString("0"));

                    double[] t3 = t.Item3;
                    if (t3.Length <= 4)
                        xml.WriteElementString("PassFreq", t3[0].ToString("0.0000"));
                    if (t3.Length > 1 && t3.Length <= 4)
                        xml.WriteElementString("StopFreq", t3[1].ToString("0.0000"));
                    if (t3.Length > 2)
                        xml.WriteElementString("StopAtten", t3[2].ToString("0.000"));
                    if (t3.Length > 3)
                        xml.WriteElementString("PassRipple", (100D * t3[3]).ToString("0.00"));
                    if (t3.Length > 4)
                    {
                        xml.WriteStartElement("NullFrequency");
                        xml.WriteAttributeString("NullNumber", ((int)t3[5]).ToString("0"));
                        xml.WriteString(t3[4].ToString("0.0000"));
                        xml.WriteEndElement(/*NullFrequency*/);
                    }
                    xml.WriteEndElement(/*IIRFilter*/);
                }
                xml.WriteEndElement(/*Filters*/);

            }

            xml.WriteElementString("OutputDecimation", ppw.SR.Decimation2.ToString("0"));

            if (ppw.doLaplacian)
            {
                xml.WriteStartElement("SurfaceLaplacian");

                xml.WriteStartElement("HeadGeometry");
                if (ppw.HeadFitOrder == 0)
                    xml.WriteAttributeString("Type", "Sphere");
                else
                {
                    xml.WriteAttributeString("Type", "Spherical harmonic");
                    xml.WriteElementString("Order", ppw.HeadFitOrder.ToString("0"));
                }
                xml.WriteEndElement(/*HeadGeometry*/);

                xml.WriteStartElement("Methodology");
                xml.WriteAttributeString("Type", ppw.NewOrleans ? "New Orleans" : "Polyharmonic spline");
                if (ppw.NewOrleans)
                    xml.WriteElementString("Lambda", ppw.NOlambda.ToString("0.00"));
                else
                {
                    xml.WriteElementString("Order", ppw.PHorder.ToString("0"));
                    xml.WriteElementString("PolyDegree", ppw.PHdegree.ToString("0"));
                    xml.WriteElementString("Lambda", ppw.PHlambda.ToString("0.00"));
                }
                xml.WriteEndElement(/*Methodology*/);

                xml.WriteStartElement("OutputLocations");
                if (ppw._outType == 1)
                    xml.WriteAttributeString("Type", "Input locations");
                else if (ppw._outType == 3)
                {
                    xml.WriteAttributeString("Type", "ETR file");
                    xml.WriteElementString("ETRFile", ppw.ETROutputFullPathName);
                }
                else //_outType == 2
                {
                    xml.WriteAttributeString("Type", "Calculated");
                    xml.WriteElementString("NominalDistance", ppw.aDist.ToString("0.00"));
                }
                xml.WriteEndElement(/*OutputLocations*/);

                xml.WriteEndElement(/*SurfaceLaplacian*/);
            }

            xml.WriteStartElement("Output");
            if (ppw.outputSFP)
                xml.WriteAttributeString("SFP", ppw.SFPcm ? "CM" : "MM");
            xml.WriteElementString("Sequence", ppw.sequenceName);
            xml.WriteEndElement(/*Output*/);

            xml.WriteEndElement(/* PreprocessorParameters */);
            xml.WriteEndDocument();
            xml.Close();
        }

        private void PerformOpenPFile()
        {
            System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog();
            dlg.Title = "Open parameter file ...";
            dlg.DefaultExt = ".par"; // Default file extension
            dlg.Filter = "PAR Files (.par)|*.par"; // Filter files by extension
            dlg.InitialDirectory = Properties.Settings.Default.LastParFile;

            bool result = dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK;
            if (!result) return;

            Properties.Settings.Default.LastParFile = System.IO.Path.GetDirectoryName(dlg.FileName);

            XmlReaderSettings xrs = new XmlReaderSettings();
            xrs.CloseInput = true;
            xrs.IgnoreWhitespace = true;
            XmlReader xml = XmlReader.Create(dlg.OpenFile(), xrs);
            try
            {
                if (!xml.ReadToFollowing("PreprocessorParameters"))
                    throw new XmlException("No PreprocessorParameters element found");
                string type = xml["Type"];
                if (type == "RWNL")
                    if (ppw.inputType != InputType.RWNL) throw new Exception("Input daataset not RWNL type");
                if (type == "BDF")
                        if (ppw.inputType != InputType.BDF) throw new Exception("Input file not BDF type");
                if (type == "SET")
                    if (ppw.inputType != InputType.SET) throw new Exception("Input file not SET type");
                xml.ReadStartElement("PreprocessorParameters");

                InputDecimation.Text = xml.ReadElementContentAsString("InputDecimation", "");

                if (xml.Name == "Detrend")
                {
                    Detrend.IsChecked = true;
                    DetrendOrder.Text = xml.ReadElementContentAsString();
                }
                else
                    Detrend.IsChecked = false;

                if (xml.Name == "Reference")
                {
                    Reference.IsChecked = true;
                    RefExclude.IsChecked = xml["ExcludeUnselected"] == "True";
                    type = xml["Type"];
                    xml.ReadStartElement(/*Reference*/);
                    if (type == "Channels")
                    {
                        RefSelectedChan.IsChecked = true;
                        RefChan.Text = xml.ReadElementContentAsString("RefChannels", "");
                    }
                    else if (type == "Expression")
                    {
                        RefExpression.IsChecked = true;
                        RefChanExpression.Text = xml.ReadElementContentAsString("RefExpression", "");
                    }
                    else
                    {
                        RefMatrix.IsChecked = true;
                        RefMatrixFile.Text = xml.ReadElementContentAsString("RefMatrixFile", "");
                    }
                    xml.ReadEndElement(/*Reference*/);
                }
                else
                    Reference.IsChecked = false;

                if (xml.Name == "Filters")
                {
                    Filtering.IsChecked = true;
                    FilterList.Items.Clear();
                    ZP.IsChecked = xml["Causal"] == "False";
                    xml.ReadStartElement();
                    while (xml.Name == "IIRFilter")
                    {
                        type = xml["Type"];
                        int k = type.IndexOf(' ');
                        string st = type.Substring(0, k);
                        xml.ReadStartElement();
                        if (st == "Elliptic")
                        {
                            AddElliptic_Click(null, null);
                            int c = FilterList.Items.Count - 1; //No Last
                            EllipticDesignControl edc = (EllipticDesignControl)FilterList.Items[c];
                            st = type.Substring(k + 1);
                            if (st == "LPZF")
                            {
                                edc.ZFSpecial.IsChecked = true;
                                edc.ZFNP.Text = xml.ReadElementContentAsString("Poles", "");
                                edc.ZFAttenS.Text = xml.ReadElementContentAsString("StopAtten", "");
                                edc.ZFRipple.Text = xml.ReadElementContentAsString("PassRipple", "");
                                edc.ZFNNull.Text = xml["NullNumber"];
                                edc.ZFF.Text = xml.ReadElementContentAsString("NullFrequency", "");
                            }
                            else
                            {
                                if (st == "HP")
                                    edc.HighPass.IsChecked = true;
                                else edc.LowPass.IsChecked = true;
                                edc.PolesCB.IsChecked = true;
                                edc.PassFCB.IsChecked = true;
                                edc.StopFCB.IsChecked = true;
                                edc.StopACB.IsChecked = false;
                                edc.RippleCB.IsChecked = true;
                                edc.Poles.Text = xml.ReadElementContentAsString("Poles", "");
                                edc.Cutoff.Text = xml.ReadElementContentAsString("PassFreq", "");
                                edc.StopF.Text = xml.ReadElementContentAsString("StopFreq", "");
                                xml.ReadElementContentAsString("StopAtten", ""); //skip
                                edc.Ripple.Text = xml.ReadElementContentAsString("PassRipple", "");
                            }
                        }
                        else if (st == "Chebyshev2")
                        {
                            AddChebyshevII_Click(null, null);
                            int c = FilterList.Items.Count - 1; //No Last
                            Chebyshev2DesignControl cdc = (Chebyshev2DesignControl)FilterList.Items[c];
                            st = type.Substring(k + 1);
                            if (st == "HP")
                                cdc.HighPass.IsChecked = true;
                            else cdc.LowPass.IsChecked = true;
                            cdc.PolesCB.IsChecked = true;
                            cdc.CutoffCB.IsChecked = true;
                            cdc.StopFCB.IsChecked = true;
                            cdc.StopACB.IsChecked = false;
                            cdc.Poles.Text = xml.ReadElementContentAsString("Poles", "");
                            cdc.Cutoff.Text = xml.ReadElementContentAsString("PassFreq", "");
                            cdc.StopF.Text = xml.ReadElementContentAsString("StopFreq", "");
                            xml.ReadElementContentAsString("StopAtten", ""); //skip
                        }
                        else if (st == "Butterworth")
                        {
                            AddButterworth_Click(null, null);
                            int c = FilterList.Items.Count - 1; //No Last
                            ButterworthDesignControl edc = (ButterworthDesignControl)FilterList.Items[c];
                            st = type.Substring(k + 1);
                            if (st == "HP")
                                edc.HP.IsChecked = true;
                            else edc.LP.IsChecked = true;
                            edc.PolesCB.IsChecked = true;
                            edc.CutoffCB.IsChecked = true;
                            edc.StopCB.IsChecked = false;
                            edc.Poles.Text = xml.ReadElementContentAsString("Poles", "");
                            edc.Cutoff.Text = xml.ReadElementContentAsString("PassFreq", "");
                        }
                        xml.ReadEndElement(/*IIRFilter*/);
                    }
                    xml.ReadEndElement(/*Filters*/);
                }
                else
                    Filtering.IsChecked = false;

                OutputDecimation.Text = xml.ReadElementContentAsString("OutputDecimation", "");

                if (xml.Name == "SurfaceLaplacian")
                {
                    Laplacian.IsChecked = true;
                    xml.ReadStartElement(/*SurfaceLaplacian*/);
                    type = xml["Type"];
                    xml.ReadStartElement("HeadGeometry");
                    if (type == "Sphere")
                    {
                        Spherical.IsChecked = true;
                    }
                    else
                    {
                        Fitted.IsChecked = true;
                        FitOrder.Text = xml.ReadElementContentAsString("Order", "");
                        xml.ReadEndElement();
                    }

                    type = xml["Type"];
                    xml.ReadStartElement("Methodology");
                    if (type == "New Orleans")
                    {
                        NO.IsChecked = true;
                        NOLambda.Text = xml.ReadElementContentAsString("Lambda", "");
                    }
                    else //Polyharmonic spline
                    {
                        PolySpline.IsChecked = true;
                        PolyHarmOrder.Text = xml.ReadElementContentAsString("Order", "");
                        PolyHarmDegree.Text = xml.ReadElementContentAsString("PolyDegree", "");
                        PolyHarmLambda.Text = xml.ReadElementContentAsString("Lambda", "");
                    }
                    xml.ReadEndElement(/*Methodology*/);

                    type = xml["Type"];
                    if (type == "Input locations")
                    {
                        Current.IsChecked = true;
                        xml.ReadElementContentAsString("OutputLocations", "");
                    }
                    else
                    {
                        xml.ReadStartElement("OutputLocations");
                        if (type == "Calculated")
                        {
                            AButt.IsChecked = true;
                            ArrayDist.Text = xml.ReadElementContentAsString("NominalDistance", "");
                        }
                        else //ETR file
                        {
                            Other.IsChecked = true;
                            LaplaceETR.Text = xml.ReadElementContentAsString("ETRFile", "");
                        }
                        xml.ReadEndElement(/*OutputLocations*/);
                    }
                    xml.ReadEndElement(/*SurfaceLaplacian*/);
                }
                else
                    Laplacian.IsChecked = false;

                string sfp = xml["SFP"];
                if (sfp == null || sfp == "False")
                    CreateSFP.IsChecked = false;
                else
                {
                    CreateSFP.IsChecked = true;
                    Cmmm.SelectedIndex = xml["SFP"] == "MM" ? 1 : 0;
                }
                xml.ReadStartElement("Output");
                SequenceName.Text = xml.ReadElementContentAsString("Sequence", "");
                xml.ReadEndElement(/*Output*/);

                xml.ReadEndElement(/* PreprocessorParameters */);
            }
            catch (XmlException e)
            {
                ErrorWindow er = new ErrorWindow();
                er.Message = "Error in parameter file at line number " + e.LineNumber.ToString("0") + ". Unable to continue.";
                er.ShowDialog();
            }
            catch (Exception e)
            {
                ErrorWindow er = new ErrorWindow();
                er.Message = "Exception in parameter file: " + e.Message;
                er.ShowDialog();
            }
            xml.Close();
        }
    }
}
