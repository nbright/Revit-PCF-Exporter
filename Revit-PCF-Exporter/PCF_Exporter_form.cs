﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BuildingCoder;
using Excel;
using PCF_Parameters;
using PCF_Functions;
using mySettings = PCF_Functions.Properties.Settings;
using iv = PCF_Functions.InputVars;

namespace PCF_Exporter
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public partial class PCF_Exporter_form : System.Windows.Forms.Form
    {
        public static ExternalCommandData _commandData;
        public static UIApplication _uiapp;
        public static UIDocument _uidoc;
        public static Document _doc;
        public string _message;
        public string _excelPath = null;
        private IList<string> PCF_DATA_TABLE_NAMES = new List<string>();
        public DataSet DATA_SET = null;
        public static DataTable DATA_TABLE = null;

        public PCF_Exporter_form(ExternalCommandData cData, string message)
        {
            InitializeComponent();
            _commandData = cData;
            _uiapp = _commandData.Application;
            _uidoc = _uiapp.ActiveUIDocument;
            _doc = _uidoc.Document;
            _message = message;

            //Init excel path
            _excelPath = mySettings.Default.excelPath;
            //textBox20.Text = _excelPath;
            //if (!mySettings.Default.excelWorksheetNames.IsNullOrEmpty())
            //{
            //    PCF_DATA_TABLE_NAMES = mySettings.Default.excelWorksheetNames;
            //    comboBox1.DataSource = PCF_DATA_TABLE_NAMES;
            //    comboBox1.SelectedIndex = PCF_DATA_TABLE_NAMES.IndexOf(mySettings.Default.excelWorksheetSelectedName);
            //}

            //Initialize dataset at loading of form if path is not null or empty. Add handling for bad path if file does not exist. Maybe in the populator class.
            //try
            //{
            //    if (!string.IsNullOrEmpty(_excelPath))
            //    {
            //        FileStream stream = File.Open(_excelPath, FileMode.Open, FileAccess.Read);
            //        IExcelDataReader excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream);
            //        //First row is column names in dataset
            //        excelReader.IsFirstRowAsColumnNames = true;
            //        DATA_SET = excelReader.AsDataSet();
            //        //DataTableCollection PCF_DATA_TABLES = DATA_SET.Tables;
            //        excelReader.Close();
            //        iv.ExcelSheet = (string)comboBox1.SelectedItem;
            //        DATA_TABLE = DATA_SET.Tables[iv.ExcelSheet];
            //        ParameterData.parameterNames = null;
            //        ParameterData.parameterNames = (from dc in DATA_TABLE.Columns.Cast<DataColumn>() select dc.ColumnName).ToList();
            //        ParameterData.parameterNames.RemoveAt(0);
            //    }
            //}
            //catch (Exception e)
            //{
            //    Util.ErrorMsg("Initialization of EXCEL data threw an exception: \n"+e.Message+"\nPlease reselect EXCEL workbook.");
            //}

            //Init Scope
            iv.SysAbbr = mySettings.Default.textBox3SpecificPipeline;
            iv.ExportAll = mySettings.Default.radioButton1AllPipelines;
            if (iv.ExportAll)
            {
                textBox3.Visible = false;
                textBox4.Visible = false;
            }
            
            //Init Bore
            iv.UNITS_BORE_MM = mySettings.Default.radioButton3BoreMM;
            iv.UNITS_BORE_INCH = mySettings.Default.radioButton4BoreINCH;
            iv.UNITS_BORE = iv.UNITS_BORE_MM ? "MM" : "INCH";
            
            //Init cooords
            iv.UNITS_CO_ORDS_MM = mySettings.Default.radioButton5CoordsMm;
            iv.UNITS_CO_ORDS_INCH = mySettings.Default.radioButton6CoordsInch;
            iv.UNITS_CO_ORDS = iv.UNITS_CO_ORDS_MM ? "MM" : "INCH";

            //Init weight
            iv.UNITS_WEIGHT_KGS= mySettings.Default.radioButton7WeightKgs;
            iv.UNITS_WEIGHT_LBS = mySettings.Default.radioButton8WeightLbs;
            iv.UNITS_WEIGHT = iv.UNITS_WEIGHT_KGS? "KGS" : "LBS";

            //Init weight-length
            iv.UNITS_WEIGHT_LENGTH_METER = mySettings.Default.radioButton9WeightLengthM;
            iv.UNITS_WEIGHT_LENGTH_FEET = mySettings.Default.radioButton10WeightLengthF;
            iv.UNITS_WEIGHT_LENGTH = iv.UNITS_WEIGHT_LENGTH_METER ? "METER" : "FEET";

            //Init output path
            iv.OutputDirectoryFilePath = mySettings.Default.textBox5OutputPath;
            textBox5.Text = iv.OutputDirectoryFilePath;

            //Init diameter limit
            iv.DiameterLimit = double.Parse(mySettings.Default.textBox22DiameterLimit);

            //Debug
            textBox8.Text = "SysAbbr: " + iv.SysAbbr;
            textBox11.Text = "ExportAll: " + iv.ExportAll;
            textBox9.Text = "BORE-MM: " + iv.UNITS_BORE_MM + iv.UNITS_BORE;
            textBox12.Text = "BORE-INCH: " + iv.UNITS_BORE_INCH + iv.UNITS_BORE;
            textBox10.Text = "COORDS-MM: " + iv.UNITS_CO_ORDS_MM + iv.UNITS_CO_ORDS;
            textBox13.Text = "COORDS-INCH: " + iv.UNITS_CO_ORDS_INCH + iv.UNITS_CO_ORDS;
            textBox14.Text = "WEIGHT-KGS: " + iv.UNITS_WEIGHT_KGS + iv.UNITS_WEIGHT;
            textBox15.Text = "WEIGHT-LBS: " + iv.UNITS_WEIGHT_LBS + iv.UNITS_WEIGHT;
            textBox16.Text = "WEIGHT-L-M: " + iv.UNITS_WEIGHT_LENGTH_METER + iv.UNITS_WEIGHT_LENGTH;
            textBox17.Text = "WEIGHT-L-F: " + iv.UNITS_WEIGHT_LENGTH_FEET + iv.UNITS_WEIGHT_LENGTH;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //Get excel file
                _excelPath = openFileDialog1.FileName;
                textBox20.Text = _excelPath;
                //Save excel file to settings
                mySettings.Default.excelPath = _excelPath;
                //Proceed to read the file
                FileStream stream = File.Open(_excelPath, FileMode.Open, FileAccess.Read);
                IExcelDataReader excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                //First row is column names in dataset
                excelReader.IsFirstRowAsColumnNames = true;
                DATA_SET = excelReader.AsDataSet();
                DataTableCollection PCF_DATA_TABLES = DATA_SET.Tables;
                PCF_DATA_TABLE_NAMES.Clear();
                foreach (DataTable VARIABLE in PCF_DATA_TABLES)
                {
                    PCF_DATA_TABLE_NAMES.Add(VARIABLE.TableName);
                }
                excelReader.Close();
                comboBox1.DataSource = PCF_DATA_TABLE_NAMES;
                //Save to settings
                //mySettings.Default.excelWorksheetNames = PCF_DATA_TABLE_NAMES;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            CreateParameterBindings CPB = new CreateParameterBindings();
            CPB.CreateElementBindings(_uiapp, ref _message);
            CPB.CreatePipelineBindings(_uiapp, ref _message);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DeleteParameters DP = new DeleteParameters();
            DP.ExecuteMyCommand(_uiapp, ref _message);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            PopulateParameters PP = new PopulateParameters();
            PP.PopulateElementData(_uiapp, ref _message, _excelPath);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            PopulateParameters PP = new PopulateParameters();
            PP.PopulatePipelineData(_uiapp, ref _message, _excelPath);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            iv.ExcelSheet = (string) comboBox1.SelectedItem;
            //mySettings.Default.excelWorksheetSelectedName = iv.ExcelSheet;
            DATA_TABLE = DATA_SET.Tables[iv.ExcelSheet];
            ParameterData.parameterNames = null;
            ParameterData.parameterNames = (from dc in DATA_TABLE.Columns.Cast<DataColumn>() select dc.ColumnName).ToList();
            ParameterData.parameterNames.RemoveAt(0);
            Util.InfoMsg("Following parameters will be initialized:\n"+ string.Join("\n", ParameterData.parameterNames.ToArray()));
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            iv.SysAbbr = textBox3.Text;
            textBox8.Text = "SysAbbr: " + iv.SysAbbr;
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked == true)
            {
                iv.ExportAll = true;
                textBox3.Visible = false; textBox4.Visible = false;
                textBox11.Text = "ExportAll: " + iv.ExportAll;
                }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked == true)
            {
                iv.ExportAll = false;
                textBox3.Visible = true; textBox4.Visible = true;
                textBox11.Text = "ExportAll: " + iv.ExportAll;
            }
           
        }

        private void button5_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog();
            if (result == DialogResult.OK)
            {
            iv.OutputDirectoryFilePath = fbd.SelectedPath;
            textBox5.Text = iv.OutputDirectoryFilePath;
            mySettings.Default.textBox5OutputPath = iv.OutputDirectoryFilePath;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            PCFExport pcfExporter = new PCFExport();
            Result result = pcfExporter.ExecuteMyCommand(_uiapp, ref _message);
            if (result == Result.Succeeded) Util.InfoMsg("PCF data exported successfully!");
            if (result == Result.Failed) Util.InfoMsg("PCF data export failed for some reason.");
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton3.Checked == true)
            {
                iv.UNITS_BORE_MM = true;
                iv.UNITS_BORE_INCH = false;
                iv.UNITS_BORE = "MM";
                //debug
                textBox9.Text = "BORE-MM: " + iv.UNITS_BORE_MM + iv.UNITS_BORE;
                textBox12.Text = "BORE-INCH: " + iv.UNITS_BORE_INCH + iv.UNITS_BORE;
            }
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton4.Checked == true)
            {
                iv.UNITS_BORE_MM = false;
                iv.UNITS_BORE_INCH = true;
                iv.UNITS_BORE = "INCH";
                //Debug
                textBox9.Text = "BORE-MM: " + iv.UNITS_BORE_MM+iv.UNITS_BORE;
                textBox12.Text = "BORE-INCH: " + iv.UNITS_BORE_INCH+iv.UNITS_BORE;
            }
        }

        private void radioButton5_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton5.Checked == true)
            {
                iv.UNITS_CO_ORDS_MM = true;
                iv.UNITS_CO_ORDS_INCH = false;
                iv.UNITS_CO_ORDS = "MM";
                //Debug
                textBox10.Text = "COORDS-MM: " + iv.UNITS_CO_ORDS_MM + iv.UNITS_CO_ORDS;
                textBox13.Text = "COORDS-INCH: " + iv.UNITS_CO_ORDS_INCH + iv.UNITS_CO_ORDS;
            }
        }

        private void radioButton6_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton6.Checked == true)
            {
                iv.UNITS_CO_ORDS_MM = false;
                iv.UNITS_CO_ORDS_INCH = true;
                iv.UNITS_CO_ORDS = "INCH";
                //Debug
                textBox10.Text = "COORDS-MM: " + iv.UNITS_CO_ORDS_MM + iv.UNITS_CO_ORDS;
                textBox13.Text = "COORDS-INCH: " + iv.UNITS_CO_ORDS_INCH + iv.UNITS_CO_ORDS;
            }
        }

        private void radioButton7_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton7.Checked == true)
            {
                iv.UNITS_WEIGHT_KGS = true;
                iv.UNITS_WEIGHT_LBS = false;
                iv.UNITS_WEIGHT = "KGS";
                //Debug
                textBox14.Text = "WEIGHT-KGS: " + iv.UNITS_WEIGHT_KGS + iv.UNITS_WEIGHT;
                textBox15.Text = "WEIGHT-LBS: " + iv.UNITS_WEIGHT_LBS + iv.UNITS_WEIGHT;
            }
        }

        private void radioButton8_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton8.Checked == true)
            {
                iv.UNITS_WEIGHT_KGS = false;
                iv.UNITS_WEIGHT_LBS = true;
                iv.UNITS_WEIGHT = "LBS";
                //Debug
                textBox14.Text = "WEIGHT-KGS: " + iv.UNITS_WEIGHT_KGS + iv.UNITS_WEIGHT;
                textBox15.Text = "WEIGHT-LBS: " + iv.UNITS_WEIGHT_LBS + iv.UNITS_WEIGHT;
            }
        }
        
        private void radioButton9_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton9.Checked == true)
            {
                iv.UNITS_WEIGHT_LENGTH_METER = true;
                iv.UNITS_WEIGHT_LENGTH_FEET = false;
                iv.UNITS_WEIGHT_LENGTH = "METER";
                //Debug
                textBox16.Text = "WEIGHT-L-M: " + iv.UNITS_WEIGHT_LENGTH_METER + iv.UNITS_WEIGHT_LENGTH;
                textBox17.Text = "WEIGHT-L-F: " + iv.UNITS_WEIGHT_LENGTH_FEET + iv.UNITS_WEIGHT_LENGTH;
            }
        }

        private void radioButton10_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton10.Checked == true)
            {
                iv.UNITS_WEIGHT_LENGTH_METER = false;
                iv.UNITS_WEIGHT_LENGTH_FEET = true;
                iv.UNITS_WEIGHT_LENGTH = "FEET";
                //Debug
                textBox16.Text = "WEIGHT-L-M: " + iv.UNITS_WEIGHT_LENGTH_METER + iv.UNITS_WEIGHT_LENGTH;
                textBox17.Text = "WEIGHT-L-F: " + iv.UNITS_WEIGHT_LENGTH_FEET + iv.UNITS_WEIGHT_LENGTH;
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            ScheduleCreator SC = new ScheduleCreator();
            var output = SC.CreateAllItemsSchedule(_uidoc);
            
            if (output == Result.Succeeded) Util.InfoMsg("Schedules created successfully!");
            else if (output == Result.Failed) Util.InfoMsg("Schedule creation failed for some reason.");
        }

        private void button9_Click(object sender, EventArgs e)
        {
            ExportParameters EP = new ExportParameters();
            var output = EP.ExecuteMyCommand(_uiapp);
            if (output == Result.Succeeded) Util.InfoMsg("Elements exported to EXCEL successfully!");
            else if (output == Result.Failed) Util.InfoMsg("Element export to EXCEL failed for some reason.");
        }

        private void richTextBox1_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            Process.Start(e.LinkText);
        }

        private void textBox22_TextChanged(object sender, EventArgs e)
        {
            iv.DiameterLimit = double.Parse(textBox22.Text);
        }
    }
}