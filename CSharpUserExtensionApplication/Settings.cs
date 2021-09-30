﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ZOSAPI;
using ZOSAPI.Editors;
using ZOSAPI.Editors.LDE;
using ZOSAPI.Tools;
using ZOSAPI.Tools.General;
using ZOSAPI.Tools.Optimization;

namespace Reverse_SLM
{
    public partial class Settings : Form
    {
        public Settings()
        {
            InitializeComponent();

            // This loads up the settings form, and populates the settings with the specified values
            // Put all intialization scripts in here (including loading setting values from previous run)
            // Create a connection to OpticStudio in order to get information from 'Program.cs'
            TheConnection = new ZOSAPI_Connection();
            TheApplication = TheConnection.ConnectToApplication();
            TheSystem = TheApplication.PrimarySystem;

            // Data directory
            string data_directory = TheApplication.ZemaxDataDir;

            // Path to stock lens vendor catalogs
            string[] vendors_folder = { data_directory, "Stockcat" };
            string vendors_path = Path.Combine(vendors_folder);

            // Get all catalog paths
            string[] vendor_files = Directory.GetFiles(vendors_path, "*.ZMF");

            // Extract the catalog filename only (this is the vendor)
            string[] vendors = new string[vendor_files.Length];

            for (int ii = 0; ii < vendor_files.Length; ii++)
            {
                vendors[ii] = Path.GetFileNameWithoutExtension(vendor_files[ii]);
            }

            lbxVendors.Items.Clear();
            lbxVendors.Items.AddRange(vendors);

            comboCycles.SelectedIndex = 0;
        }

        public bool Terminate = false;

        public ZOSAPI_Connection TheConnection;
        public IZOSAPI_Application TheApplication;
        public IOpticalSystem TheSystem;

        private void cbxVendors_CheckedChanged(object sender, EventArgs e)
        {
            if (cbxVendors.Checked)
            {
                lbxVendors.Enabled = false;
            }
            else
            {
                lbxVendors.Enabled = true;
            }
        }

        private void cbxAirThicknessCompensation_CheckedChanged(object sender, EventArgs e)
        {
            if (cbxAirThicknessCompensation.Checked)
            {
                comboCycles.Enabled = true;
            }
            else
            {
                comboCycles.Enabled = false;
            }
        }

        private void tolerance_char_validation(object sender, KeyPressEventArgs e)
        {
            // Retrieve calling textbox
            TextBox toleranceCaller = sender as TextBox;

            // Current inpput char in the textbox
            char inputChar = e.KeyChar;

            // Force a maximum of one dot in the textbox
            if (inputChar == charCodes.DOT && toleranceCaller.Text.IndexOf('.') != -1)
            {
                // Skip the current input char
                e.Handled = true;
                return;
            }

            // Characters that are not BACKSPACE or DOT
            if (inputChar != charCodes.BACKSPACE && inputChar != charCodes.DOT)
            {
                // and that are not DIGITs
                if (!char.IsDigit(inputChar))
                {
                    // are skipped
                    e.Handled = true;
                }
                else
                {
                    // if the character is a digit, and there's already a dot
                    if (toleranceCaller.Text.IndexOf('.') == 0 || toleranceCaller.Text.IndexOf('.') == 1)
                    {
                        // keep only one decimal
                        if (toleranceCaller.Text.Length > toleranceCaller.Text.IndexOf('.') + 1)
                        {
                            // and skip the remaining decimals
                            e.Handled = true;
                        }
                    }
                }
            }
        }

        private void tolerance_max_validation(object sender, EventArgs e)
        {
            float tolerance;

            // Retrieve calling textbox
            TextBox toleranceCaller = sender as TextBox;

            // Check if the value is greater than 100
            if (float.TryParse(toleranceCaller.Text, out tolerance))
            {
                // Set maximum value to 100
                if (tolerance > 100)
                {
                    toleranceCaller.Text = "100";
                }
            }
            else
            {
                // If the value can't be read, set it to a default of 25
                toleranceCaller.Text = "25";
            }
        }

        private void numMatches_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Current inpput char in the textbox
            char inputChar = e.KeyChar;

            // Characters that are not DIGITs or BACKSPACE
            if (!char.IsDigit(inputChar) && inputChar != charCodes.BACKSPACE)
            {
                // are skipped
                e.Handled = true;
            }
        }

        private void numMatches_Leave(object sender, EventArgs e)
        {
            // Retrieve calling numeric
            NumericUpDown numericCaller = sender as NumericUpDown;

            // If empty
            if (numericCaller.Text == "")
            {
                // setup a default number of matches of 5
                numericCaller.Text = "5";
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnLaunch_Click(object sender, EventArgs e)
        {
            // Enable/disable buttons
            btnLaunch.Enabled = false;
            btnCancel.Enabled = false;
            btnTerminate.Enabled = true;

            // Update progress
            TheApplication.ProgressPercent = 0;
            TheApplication.ProgressMessage = "Reading settings ...";

            // Flags
            bool skipped = false;
            bool hasVar = false;
            bool insertionError = false;
            bool insertionErrorFlag = false;
            bool materialError = false;
            bool materialErrorFlag = false;

            // Variables
            int elemCount = 0;
            int lenStart = -1;
            int totalMatches = -1;
            int vendorMatches = -1;
            double EFL;
            double EPD = -1.0;
            double nominalMF, reverseMF, curMF, thicknessAfter;
            string curMaterial, prevMaterial, mfPath, tempMess;
            ILensDataEditor TheLDE = TheSystem.LDE;
            ISolveData ThicknessSolve;
            ILensCatalogs TheLensCatalog;
            ILensCatalogLens MatchedLens;
            IMaterialsCatalog TheMaterialsCatalog;

            // Constants
            const int ALL = 0;
            const int VARIABLE = 1;
            const int MAX_ELEMENTS = 3;

            // Retrieve settings
            int surfaceSelection, matches, optimizationCycles, numWaves;
            double eflTolerance, epdTolerance, maxWave, minWave, curWave;
            bool airCompensation, saveBest, reverseElements;
            string bestPath;
            string[] vendors;

            // Settings for catalog lens insertion
            bool ignoreObject = true;
            bool reverseGeometry = true;

            // Elements to consider for matching
            if (rbtnSurfacesAll.Checked)
            {
                // All elements
                surfaceSelection = ALL;
            }
            else if (rbtnSurfacesVariable.Checked)
            {
                // Only elements whose surfaces have at least one variable radius
                surfaceSelection = VARIABLE;
            }
            else
            {
                // Should never go to this case
                surfaceSelection = -1;
            }

            // Vendors
            if (cbxVendors.Checked)
            {
                // All vendors
                vendors = lbxVendors.Items.OfType<string>().ToArray();
            }
            else
            {
                // Verify at least one vendor is selected
                if (lbxVendors.SelectedItems.Count == 0)
                {
                    MessageBox.Show("Select at least one vendor", "Vendor error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);

                    // Enable/disable buttons
                    btnLaunch.Enabled = true;
                    btnCancel.Enabled = true;
                    btnTerminate.Enabled = false;

                    return;
                }

                // Selected vendors
                vendors = lbxVendors.SelectedItems.OfType<string>().ToArray();
            }

            // How many matches to show?
            matches = (int)numMatches.Value;

            // What are the tolerances?
            eflTolerance = float.Parse(tbxEFL.Text);
            epdTolerance = float.Parse(tbxEPD.Text);

            // Air thickness compensation?
            if (cbxAirThicknessCompensation.Checked)
            {
                airCompensation = true;
                optimizationCycles = comboCycles.SelectedIndex;
            }
            else
            {
                airCompensation = false;
                optimizationCycles = -1;
            }

            // Save best combination?
            if (cbxSaveBest.Checked)
            {
                saveBest = true;

                // Best combination file path
                bestPath = TheSystem.SystemFile;
                string directory = Path.GetDirectoryName(bestPath);
                string bestName = Path.GetFileNameWithoutExtension(bestPath);
                bestPath = Path.Combine(directory, bestName + "_RSLM.ZMX");
            }
            else
            {
                saveBest = false;
                bestPath = "";
            }

            // Utilize element reversal?
            reverseElements = cbxReverse.Checked;

            // Retrieve maximum, and minimum wavelengths (used later to check if a matched lens material is compatible)
            maxWave = double.NegativeInfinity;
            minWave = double.PositiveInfinity;
            numWaves = TheSystem.SystemData.Wavelengths.NumberOfWavelengths;

            for (int ii = 1; ii <= numWaves; ii++)
            {
                curWave = TheSystem.SystemData.Wavelengths.GetWavelength(ii).Wavelength;

                if (curWave > maxWave)
                {
                    maxWave = curWave;
                }

                if (curWave < minWave)
                {
                    minWave = TheSystem.SystemData.Wavelengths.GetWavelength(ii).Wavelength;
                }
            }

            // Data directory
            string dataDir = TheApplication.ZemaxDataDir;

            // First and last surfaces
            int firstSurf = 1;
            int lastSurf = TheLDE.NumberOfSurfaces - 1;

            // Initialization of list of nominal lenses
            List<idealLens> nominalLenses = new List<idealLens>();

            // Start searching nominal lenses from air
            prevMaterial = "";

            // Loop over the system surfaces to find nominal lenses
            for (int surfID = firstSurf; surfID < lastSurf; surfID++)
            {
                // Retrieve current surface
                ILDERow curSurf = TheLDE.GetSurfaceAt(surfID);

                // Retrieve current material
                try
                {
                    curMaterial = curSurf.Material;
                }
                catch
                {
                    // If current material is air: returns a null object, which triggers this exception
                    // Note: this also means the application doesn't treat Model Glass solves or
                    // any sort of solve for that matter
                    curMaterial = "";
                }

                // Were we in air?
                if (prevMaterial == "")
                {
                    // Are we still in air?
                    if (curMaterial == "")
                    {
                        // Then skip this surface
                        continue;
                    }
                    // Is it a mirror?
                    else if (curMaterial == "MIRROR")
                    {
                        // Then skip this surface as well
                        continue;
                    }
                    // Otherwise start a new lens
                    else
                    {
                        // Set new lens start surface
                        lenStart = surfID;

                        // EPD is twice the new surface clear semi-diameter
                        EPD = 2 * curSurf.SemiDiameter;

                        // Check if surface has variable radius
                        if (curSurf.RadiusCell.GetSolveData().Type == ZOSAPI.Editors.SolveType.Variable)
                        {
                            hasVar = true;
                        }

                        // Update counter
                        elemCount = 1;
                    }
                }
                else
                {
                    // Are we completing a lens, i.e. returning in air?
                    if (curMaterial == "")
                    {
                        // If it has more than MAX_ELEMENS, skip the lens
                        if (elemCount > MAX_ELEMENTS)
                        {
                            // Update counter
                            elemCount = 0;
                            skipped = true;
                            continue;
                        }

                        // Does the final element has a variable radius?
                        if (curSurf.RadiusCell.GetSolveData().Type == ZOSAPI.Editors.SolveType.Variable)
                        {
                            hasVar = true;
                        }

                        // If it doesn't have at least one variable radius and variable is true, skip the lens
                        if (surfaceSelection == VARIABLE && !hasVar)
                        {
                            // Update counter
                            elemCount = 0;
                            continue;
                        }

                        // Retrieve effective focal length
                        EFL = TheSystem.MFE.GetOperandValue(ZOSAPI.Editors.MFE.MeritOperandType.EFLX, 
                            lenStart, surfID, 0, 0, 0, 0, 0, 0);

                        // Update list of lenses
                        nominalLenses.Add(new idealLens(elemCount, lenStart, EFL, EPD));

                        // Update counter and flag
                        elemCount = 0;
                        hasVar = false;
                    }
                    // Is it a new element of the lens (I'm not exactly sure how to treat two consecutive
                    // surfaces with the same material, so it is currently unsupported), that is not a mirror?
                    else if ((curMaterial != prevMaterial) && (curMaterial != "MIRROR"))
                    {
                        // Has the EPD increased?
                        if (2 * curSurf.SemiDiameter > EPD)
                        {
                            // If so, update the EPD
                            EPD = 2 * curSurf.SemiDiameter;
                        }

                        // Does the new element has a variable radius?
                        if (curSurf.RadiusCell.GetSolveData().Type == ZOSAPI.Editors.SolveType.Variable)
                        {
                            hasVar = true;
                        }

                        // Update counter
                        elemCount++;
                    }
                    else
                    {
                        // Then skip this surface (this includes the case where a lens is directly followed
                        // by a mirror, I don't know why anyone would do that but just in case)
                        continue;
                    }
                }

                // Update previous material
                prevMaterial = curMaterial;
            }

            if (nominalLenses.Count == 0)
            {
                MessageBox.Show("No lenses found for matching", "Lens error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);

                // Enable/disable buttons
                btnLaunch.Enabled = true;
                btnCancel.Enabled = true;
                btnTerminate.Enabled = false;

                return;
            }

            // Create a temporary file to evaluate the matches
            string[] tempFolder = { dataDir, "DeleteMe.ZMX" };
            string tempPath = Path.Combine(tempFolder);

            // Get nominal MF
            nominalMF = TheSystem.MFE.CalculateMeritFunction();

            // Save variable air thicknesses
            List<int> airThicknesses = varAirThicknesses(TheLDE, lastSurf);

            // Save the system
            TheSystem.Save();

            // Save the MF
            mfPath = Path.Combine(dataDir, "DeleteMe.MF");
            TheSystem.MFE.SaveMeritFunction(mfPath);

            // Open a copy of the system
            IOpticalSystem TheSystemCopy = TheSystem.CopySystem();

            // Copy of lens data editor
            ILensDataEditor TheLDECopy = TheSystemCopy.LDE;

            // Remove all variables
            TheSystemCopy.Tools.RemoveAllVariables();

            // Create a thickness variable solve
            ThicknessSolve = TheLDECopy.GetSurfaceAt(0).ThicknessCell.CreateSolveType(ZOSAPI.Editors.SolveType.Variable);

            // Have only previous variable air thicknesses as new variables
            foreach (int surface_id in airThicknesses)
            {
                TheLDECopy.GetSurfaceAt(surface_id).ThicknessCell.SetSolveData(ThicknessSolve);
            }

            // Save the copy of the system with correct variables
            TheSystemCopy.SaveAs(tempPath);

            // Array of best matches for each nominal lens
            catalogLens[,] bestMatches = new catalogLens[nominalLenses.Count, matches];

            // Loop over the lenses to be matched
            for (int nominalLensID = 0; nominalLensID < nominalLenses.Count; nominalLensID++)
            {
                // Initialization
                totalMatches = 0;

                // Initialize array of best matches
                // [ Nominal lens | Match ID | Reverse flag | MF value | Name | Vendor ]
                for (int ii = 0; ii < matches; ii++)
                {
                    bestMatches[nominalLensID, ii] = new catalogLens(nominalLensID, -1, false, double.PositiveInfinity, "", "");
                }

                // Current nominal lens properties
                elemCount = nominalLenses[nominalLensID].ElemCount;
                lenStart = nominalLenses[nominalLensID].StartSurf;
                EFL = nominalLenses[nominalLensID].EFL;
                EPD = nominalLenses[nominalLensID].EPD;

                // Iterate over the vendors
                for (int vendorID = 0; vendorID < vendors.Length; vendorID++)
                {
                    // Run the lens catalog tool once to get the number of matches
                    TheLensCatalog = TheSystemCopy.Tools.OpenLensCatalogs();

                    // Apply settings to catalog tool
                    applyCatalogSettings(TheLensCatalog, elemCount, EFL, EPD, eflTolerance, epdTolerance, vendors[vendorID]);

                    // Run the lens catalog tool for the given vendor
                    TheLensCatalog.RunAndWaitForCompletion();

                    // Save the number of matches
                    vendorMatches = TheLensCatalog.MatchingLenses;
                    totalMatches += vendorMatches;

                    // Close the lens catalog tool
                    TheLensCatalog.Close();

                    // Loop over the matches
                    for (int matchID = 0; matchID < vendorMatches; matchID++)
                    {
                        // Indicator to compare the reversed-element MF value
                        reverseMF = double.PositiveInfinity;

                        // Check if terminate was pressed
                        if (!this.Terminate)
                        {
                            // Run the lens catalog once again and for every match (it needs to be closed for the optimization tool to be opened)
                            // Feature request: can we have a field to search for a specific lens by its name?
                            TheLensCatalog = TheSystemCopy.Tools.OpenLensCatalogs();
                            applyCatalogSettings(TheLensCatalog, elemCount, EFL, EPD, eflTolerance, epdTolerance, vendors[vendorID]);
                            TheLensCatalog.RunAndWaitForCompletion();

                            // Retrieve the corresponding matched lens
                            MatchedLens = TheLensCatalog.GetResult(matchID);

                            // Save the air thickness before next lens
                            thicknessAfter = TheSystemCopy.LDE.GetSurfaceAt(lenStart + elemCount).Thickness;
                            ThicknessSolve = TheSystemCopy.LDE.GetSurfaceAt(lenStart + elemCount).ThicknessCell.GetSolveData();

                            // Remove ideal lens
                            TheSystemCopy.LDE.RemoveSurfacesAt(lenStart, elemCount + 1);

                            // Insert matching lens
                            if (!MatchedLens.InsertLensSeq(lenStart, ignoreObject, reverseGeometry))
                            {
                                insertionErrorFlag = true;
                            }

                            // Restore thickness
                            TheSystemCopy.LDE.GetSurfaceAt(lenStart + elemCount).Thickness = thicknessAfter;
                            TheSystemCopy.LDE.GetSurfaceAt(lenStart + elemCount).ThicknessCell.SetSolveData(ThicknessSolve);

                            // Close the lens catalog tool
                            TheLensCatalog.Close();

                            // Check that material is within wavelength bounds (has to be done after closing the lens catalog)
                            for (int materialID = 0; materialID < elemCount; materialID++)
                            {
                                // Material of every element of the lens
                                curMaterial = TheSystemCopy.LDE.GetSurfaceAt(lenStart + materialID).Material;

                                // Open the material catalog
                                TheMaterialsCatalog = TheSystemCopy.Tools.OpenMaterialsCatalog();

                                if (!materialIsCompatible(TheMaterialsCatalog, curMaterial, maxWave, minWave))
                                {
                                    totalMatches--;
                                    materialErrorFlag = true;
                                }

                                // Close the material catalog
                                TheMaterialsCatalog.Close();
                            }

                            // Load MF
                            TheSystemCopy.MFE.LoadMeritFunction(mfPath);

                            // Update progress
                            tempMess = "Matching: Lens " + (nominalLensID + 1).ToString() + "/" + nominalLenses.Count.ToString();
                            tempMess += " | Vendor " + (vendorID + 1).ToString() + "/" + (vendors.Length).ToString();
                            tempMess += " | Match " + (matchID + 1).ToString() + "/" + (vendorMatches).ToString();
                            TheApplication.ProgressMessage = tempMess;

                            // Report insertion and material errors
                            if (materialErrorFlag)
                            {
                                materialError = true;
                            }
                            if (insertionErrorFlag)
                            {
                                insertionError = true;
                            }

                            if (!materialErrorFlag && !insertionErrorFlag)
                            {
                                if (airCompensation)
                                {
                                    // Run the local optimizer
                                    ILocalOptimization TheOptimizer = TheSystemCopy.Tools.OpenLocalOptimization();
                                    TheOptimizer.Algorithm = ZOSAPI.Tools.Optimization.OptimizationAlgorithm.DampedLeastSquares;
                                    switch (optimizationCycles)
                                    {
                                        case 0:
                                            TheOptimizer.Cycles = ZOSAPI.Tools.Optimization.OptimizationCycles.Automatic;
                                            break;
                                        case 1:
                                            TheOptimizer.Cycles = ZOSAPI.Tools.Optimization.OptimizationCycles.Fixed_1_Cycle;
                                            break;
                                        case 2:
                                            TheOptimizer.Cycles = ZOSAPI.Tools.Optimization.OptimizationCycles.Fixed_5_Cycles;
                                            break;
                                        case 3:
                                            TheOptimizer.Cycles = ZOSAPI.Tools.Optimization.OptimizationCycles.Fixed_10_Cycles;
                                            break;
                                        case 4:
                                            TheOptimizer.Cycles = ZOSAPI.Tools.Optimization.OptimizationCycles.Fixed_50_Cycles;
                                            break;
                                    }
                                    TheOptimizer.RunAndWaitForCompletion();

                                    // Retrieve the MF value
                                    curMF = TheOptimizer.CurrentMeritFunction;

                                    // Lens reversal enabled?
                                    if (reverseElements)
                                    {
                                        // Reverse the matched lens
                                        TheLDECopy.RunTool_ReverseElements(lenStart, lenStart + elemCount);

                                        // Re-run the optimizer
                                        TheOptimizer.RunAndWaitForCompletion();

                                        // Retrieve the MF value
                                        reverseMF = TheOptimizer.CurrentMeritFunction;
                                    }

                                    // Close the optimizer
                                    TheOptimizer.Close();
                                }
                                else
                                {
                                    // Retrieve the MF value
                                    curMF = TheSystemCopy.MFE.CalculateMeritFunction();

                                    // Lens reversal enabled?
                                    if (reverseElements)
                                    {
                                        // Reverse the matched lens
                                        TheLDECopy.RunTool_ReverseElements(lenStart, lenStart + elemCount);

                                        // Retrieve the MF value
                                        reverseMF = TheSystemCopy.MFE.CalculateMeritFunction();
                                    }
                                }

                                if (reverseMF < curMF)
                                {
                                    // Is it a best match?
                                    isBestMatch(TheSystemCopy, saveBest, bestPath, bestMatches, nominalLensID, matchID, true, reverseMF, MatchedLens.LensName, MatchedLens.Vendor);
                                }
                                else
                                {
                                    // Is it a best match?
                                    isBestMatch(TheSystemCopy, saveBest, bestPath, bestMatches, nominalLensID, matchID, false, curMF, MatchedLens.LensName, MatchedLens.Vendor);
                                }
                            }

                            // Restore error flags for next match
                            materialErrorFlag = false;
                            insertionErrorFlag = false;

                            // Load the copy of the original system
                            TheSystemCopy.LoadFile(tempPath, false);











                        }
                        else
                        {
                            // Enable/disable buttons
                            btnLaunch.Enabled = true;
                            btnCancel.Enabled = true;
                            btnTerminate.Enabled = false;

                            return;
                        }
                    }
                }
            }






            // Path to text-file result
            string fileFullPath = TheSystem.SystemFile;
            string fileDir = Path.GetDirectoryName(fileFullPath);
            string fileName = Path.GetFileNameWithoutExtension(fileFullPath);
            string logPath = Path.Combine(fileDir, fileName + "_RSLM.TXT");

            // Initialize array of lines
            string line;
            List<string> lines = new List<string>();

            // Header
            line = "Reverse Stock Lens Matching Results\r\n";
            lines.Add(line);
            line = "File: " + TheSystem.SystemFile;
            lines.Add(line);
            line = "Title: " + TheSystem.SystemData.TitleNotes.Title;
            lines.Add(line);
            line = "Date: " + DateTime.Now + "\r\n\r\n";
            lines.Add(line);
            line = "All stock lens data is provided by the vendor.";
            lines.Add(line);
            line = "Please contact the vendor to verify the availability";
            lines.Add(line);
            line = "of the lenses selected.\r\n";
            lines.Add(line);

            // Extension settings
            switch (surfaceSelection)
            {
                case ALL:
                    line = "Surfaces:\t\tAll";
                    break;
                case VARIABLE:
                    line = "Surfaces:\t\tVariables";
                    break;
            }
            lines.Add(line);

            line = "Vendors:\t\t";
            if (cbxVendors.Checked)
            {
                line += "All";
            }
            else
            {
                string lastVendor = vendors.Last();
                foreach(string vendor in vendors)
                {
                    if (vendor.Equals(lastVendor))
                    {
                        line += vendor;
                    }
                    else
                    {
                        line += vendor + ", ";
                    }
                }
            }
            lines.Add(line);

            line = "Show Matches:\t\t" + matches.ToString();
            lines.Add(line);
            line = "EFL Tolerance (%):\t" + eflTolerance.ToString();
            lines.Add(line);
            line = "EPD Tolerance (%):\t" + epdTolerance.ToString();
            lines.Add(line);
            line = "Nominal Criterion:\t" + nominalMF + "\r\n";
            lines.Add(line);

            line = "Air Thickness Compensation:\t" + airCompensation.ToString();
            lines.Add(line);
            if (airCompensation)
            {
                line = "Optimization Cycles:\t\t";
                switch (optimizationCycles)
                {
                    case 0:
                        line += "Automatic";
                        break;
                    case 1:
                        line += "1 Cycle";
                        break;
                    case 2:
                        line += "5 Cycles";
                        break;
                    case 3:
                        line += "10 Cycles";
                        break;
                    case 4:
                        line += "50 Cycles";
                        break;
                }
                lines.Add(line);
            }
            line = "Save Best:\t\t\t" + saveBest.ToString();
            lines.Add(line);
            line = "Both orientations:\t\t" + reverseElements.ToString() + "\r\n";
            lines.Add(line);

            // Results
            line = "Number of Lenses Matched: " + nominalLenses.Count.ToString() + "\r\n";
            lines.Add(line);

            if (skipped)
            {
                line = "> WARNING: At least one lens with more than ";
                line += MAX_ELEMENTS.ToString();
                line += " elements has been ignored(unsupported) ... \r\n";
                lines.Add(line);
            }

            for (int ii = 0; ii < nominalLenses.Count; ii++)
            {
                line = "Component " + (ii + 1).ToString();
                line += " (Surfaces " + nominalLenses[ii].StartSurf.ToString() + "-";
                line += (nominalLenses[ii].StartSurf + nominalLenses[ii].ElemCount).ToString();
                line += ")\t\t\t\t\tMF Value\t\tMF Change\t\tIs Reversed?";
                lines.Add(line);

                for (int jj = 0; jj < matches; jj++)
                {
                    line = jj.ToString() + ") " + bestMatches[ii, jj].Name + "(";
                    line += bestMatches[ii, jj].Vendor + ")\t\t\t\t\t";
                    line += bestMatches[ii, jj].MatchedMF + "\t";
                    line += Math.Abs(nominalMF - bestMatches[ii, jj].MatchedMF);
                    line += "\t" + bestMatches[ii, jj].IsReversed;
                    lines.Add(line);
                }
            }

            // Write lines to text file
            File.WriteAllLines(logPath, lines.ToArray());
        }

        static public List<int> varAirThicknesses(ILensDataEditor TheLDE, int lastSurf)
        {
            List<int> SurfaceIDs = new List<int>();

            for (int ii = 1; ii < lastSurf; ii++)
            {
                if (TheLDE.GetSurfaceAt(ii).ThicknessCell.GetSolveData().Type == ZOSAPI.Editors.SolveType.Variable)
                {
                    SurfaceIDs.Add(ii);
                }
            }

            return SurfaceIDs;
        }

        static void applyCatalogSettings(ILensCatalogs TheLensCatalog, int elemCount, double EFL, double EPD, double eflTolerance, double epdTolerance, string vendor)
        {
            // Vendor
            TheLensCatalog.SelectedVendor = vendor;

            // EFL
            TheLensCatalog.UseEFL = true;

            // Check if EFL is positive
            if (EFL >= 0)
            {
                TheLensCatalog.MinEFL = EFL - EFL * eflTolerance;
                TheLensCatalog.MaxEFL = EFL + EFL * eflTolerance;
            }
            else
            {
                TheLensCatalog.MinEFL = EFL + EFL * eflTolerance;
                TheLensCatalog.MaxEFL = EFL - EFL * eflTolerance;
            }

            // EPD
            TheLensCatalog.UseEPD = true;
            TheLensCatalog.MinEPD = EPD - EPD * epdTolerance;
            TheLensCatalog.MaxEPD = EPD + EPD * epdTolerance;

            // Supported lens type
            TheLensCatalog.IncShapeEqui = true;
            TheLensCatalog.IncShapeBi = true;
            TheLensCatalog.IncShapePlano = true;
            TheLensCatalog.IncShapeMeniscus = true;
            TheLensCatalog.IncTypeSpherical = true;

            // Unsupported lens type
            TheLensCatalog.IncTypeGRIN = false;
            TheLensCatalog.IncTypeAspheric = false;
            TheLensCatalog.IncTypeToroidal = false;

            // Number of elements in the lens
            TheLensCatalog.NumberOfElements = elemCount;
        }

        static bool materialIsCompatible(IMaterialsCatalog TheMaterialsCatalog, string curMaterial, double maxWave, double minWave)
        {
            bool compatible = false;

            string[] AllCatalogs = TheMaterialsCatalog.GetAllCatalogs();
            string[] AllMaterials;

            foreach (string Catalog in AllCatalogs)
            {
                TheMaterialsCatalog.SelectedCatalog = Catalog;

                // The materials are updated if the catalog is changed, the materials catalog does not need to be Run()
                AllMaterials = TheMaterialsCatalog.GetAllMaterials();

                if (Array.IndexOf(AllMaterials, curMaterial) != -1)
                {
                    TheMaterialsCatalog.SelectedMaterial = curMaterial;

                    // Once again, after selecting the material, one has directly access to the max/min wavelengths without running the tool
                    if (maxWave <= TheMaterialsCatalog.MaximumWavelength && minWave >= TheMaterialsCatalog.MinimumWavelength)
                    {
                        compatible = true;
                    }

                    break;
                }
            }

            return compatible;
        }

        static void isBestMatch(IOpticalSystem TheSystemCopy, bool saveBest, string bestPath, catalogLens[,] bestMatches, int nominalLensID, int matchID, bool reverse, double mfValue, string lensName, string vendor)
        {
            int matches = bestMatches.GetLength(1);

            for (int ii = 0; ii < matches; ii++)
            {
                if (mfValue < bestMatches[nominalLensID, ii].MatchedMF)
                {
                    // Save if best
                    if (ii == 0 && saveBest)
                    {
                        TheSystemCopy.SaveAs(bestPath);
                    }

                    // Offset previous results
                    for (int jj = matches - 1; jj - ii > 0; jj--)
                    {
                        bestMatches[nominalLensID, jj].MatchID = bestMatches[nominalLensID, jj - 1].MatchID;
                        bestMatches[nominalLensID, jj].IsReversed = bestMatches[nominalLensID, jj - 1].IsReversed;
                        bestMatches[nominalLensID, jj].MatchedMF = bestMatches[nominalLensID, jj - 1].MatchedMF;
                        bestMatches[nominalLensID, jj].Name = bestMatches[nominalLensID, jj - 1].Name;
                        bestMatches[nominalLensID, jj].Vendor = bestMatches[nominalLensID, jj - 1].Vendor;
                    }

                    // Save new match as best
                    bestMatches[nominalLensID, ii].MatchID = matchID;
                    bestMatches[nominalLensID, ii].IsReversed = reverse;
                    bestMatches[nominalLensID, ii].MatchedMF = mfValue;
                    bestMatches[nominalLensID, ii].Name = lensName;
                    bestMatches[nominalLensID, ii].Vendor = vendor;

                    break;
                }
            }
        }

        private void btnTerminate_Click(object sender, EventArgs e)
        {
            this.Terminate = true;
        }
    }

    public class catalogLens
    {
        public catalogLens(int lensID, int matchID, bool isReserved, double matchedMF, string name, string vendor)
        {
            LensID = lensID;
            MatchID = matchID;
            IsReversed = isReserved;
            MatchedMF = matchedMF;
            Name = name;
            Vendor = vendor;
        }

        public int LensID;
        public int MatchID;
        public bool IsReversed;
        public double MatchedMF;
        public string Name;
        public string Vendor;
    }

    public class idealLens
    {
        public idealLens(int elemCount, int startSurf, double efl, double epd)
        {
            ElemCount = elemCount;
            StartSurf = startSurf;
            EFL = efl;
            EPD = epd;
        }

        public int ElemCount;
        public int StartSurf;
        public double EFL;
        public double EPD;
    }

    static class charCodes
    {
        public const int BACKSPACE = 8;
        public const int DOT = 46;
    }
}
