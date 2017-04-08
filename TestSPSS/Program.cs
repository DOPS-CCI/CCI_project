using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SPSSFile;
using GroupVarDictionary;

namespace TestSPSS
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            List<Variable> Vlist = new List<Variable>();
            GVEntry gv = new GVEntry();
            GroupVarDictionary.GroupVarDictionary gvd = new GroupVarDictionary.GroupVarDictionary();
            gvd.Add("WhatTheRabbitSaw", gv);
            gv.Description = "Whenever one does this, one \"wishes\" one hadn't done";
            gv.GVValueDictionary = new Dictionary<string, int>(3);
            gv.GVValueDictionary.Add("Pocohantis", 1);
            gv.GVValueDictionary.Add("Tallahasee", 2);
            gv.GVValueDictionary.Add("Iroquois", 3);

            SaveFileDialog sfd = new SaveFileDialog();
            if (sfd.ShowDialog() != DialogResult.OK) return;
            Console.WriteLine(sfd.FileName);

            SPSS spss = new SPSS(sfd.FileName);
            Variable v = new NumericVariable("WhatThePostmanSaw");
            spss.AddVariable(v);
            Vlist.Add(v);
            v = new StringVariable("WahHooWah", 12);
            spss.AddVariable(v);
            Vlist.Add(v);
            v = new GroupVariable("Very_Loooooooooong_Group_Variable_Name$", gv);
            spss.AddVariable(v);
            Vlist.Add(v);
            gv.GVValueDictionary = null;
            v = new GroupVariable("Repeat_of_previous.", gv, VarType.NumString);
            spss.AddVariable(v);
            Vlist.Add(v);
            v = new StringVariable("Legal$(1)", 7);
            spss.AddVariable(v);
            Vlist.Add(v);
            v = new NumericVariable("LegalName(5)");
            spss.AddVariable(v);
            Vlist.Add(v);
            Vlist[0].setValue(12.4D);
            Vlist[1].setValue("Hamster");
            Vlist[2].setValue(3);
            Vlist[3].setValue("2");
            Vlist[4].setValue(5.8);
            Vlist[5].setValue(12.55);
            spss.WriteRecord();
            Vlist[0].setValue(-3.75D);
            Vlist[1].setValue("Piglet999");
            Vlist[2].setValue(1);
            Vlist[3].setValue("Tallahasee");
            Vlist[4].setValue("Homecoming");
            Vlist[5].setValue(23.111);
            spss.WriteRecord();
            Vlist[0].setValue(5);
            Vlist[1].setValue("CowardlyLion");
            Vlist[2].setValue(2);
            Vlist[3].setValue(3);
            Vlist[4].setValue("OK");
            Vlist[5].setValue(-3);
            spss.WriteRecord();
            Vlist[0].setValue("abc");
            Vlist[1].setValue(-15.3);
            Vlist[2].setValue(2.12);
            Vlist[3].setValue(4);
            Vlist[4].setValue(17);
            Vlist[5].setValue(-12.55);
            spss.WriteRecord();
            Vlist[0].setValue(3);
            Vlist[1].setValue("12.3");
            Vlist[2].setValue("1");
            Vlist[3].setValue("3.45");
            Vlist[4].setValue(-5.3);
            Vlist[5].setValue("11.67");
            spss.WriteRecord();
            spss.Close();

            Console.ReadLine();
        }
    }
}
