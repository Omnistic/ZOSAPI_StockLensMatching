using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZOSAPI;
using ZOSAPI.Editors;
using ZOSAPI.Editors.LDE;
using ZOSAPI.Editors.MFE;
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

        public ZOSAPI_Connection TheConnection;
        public IZOSAPI_Application TheApplication;
        public IOpticalSystem TheSystem;

        CancellationTokenSource _tokenSource = null;

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

        private async void btnLaunch_Click(object sender, EventArgs e)
        {
            // Enable/disable
            rbtnSurfacesAll.Enabled = false;
            rbtnSurfacesVariable.Enabled = false;
            cbxVendors.Enabled = false;
            lbxVendors.Enabled = false;
            numMatches.Enabled = false;
            tbxEFL.Enabled = false;
            tbxEPD.Enabled = false;
            cbxAirThicknessCompensation.Enabled = false;
            comboCycles.Enabled = false;
            cbxSaveBest.Enabled = false;
            cbxReverse.Enabled = false;
            cbxIgnoreElements.Enabled = false;
            btnLaunch.Enabled = false;
            btnCancel.Enabled = false;
            btnTerminate.Enabled = true;
            btnTerminate.Focus();

            _tokenSource = new CancellationTokenSource();
            var token = _tokenSource.Token;

            try
            {
                await Task.Run(() => launchMatching(token));
            }
            catch (OperationCanceledException)
            {
                TheApplication.ProgressPercent = 0;
                TheApplication.ProgressMessage = "Matching cancelled";
            }
            finally
            {
                _tokenSource.Dispose();
            }

            // Enable/disable buttons
            rbtnSurfacesAll.Enabled = true;
            rbtnSurfacesVariable.Enabled = true;
            cbxVendors.Enabled = true;
            lbxVendors.Enabled = true;
            numMatches.Enabled = true;
            tbxEFL.Enabled = true;
            tbxEPD.Enabled = true;
            cbxAirThicknessCompensation.Enabled = true;
            comboCycles.Enabled = true;
            cbxSaveBest.Enabled = true;
            cbxReverse.Enabled = true;
            cbxIgnoreElements.Enabled = true;
            btnLaunch.Enabled = true;
            btnLaunch.Focus();
            btnCancel.Enabled = true;
            btnTerminate.Enabled = false;
        }

        public void launchMatching(CancellationToken token)
        {
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
            bool combinations = true;
            bool mfError = false;
            bool missingMatch = false;

            // Variables
            int elemCount = 0;
            int lenStart = -1;
            int totalMatches = -1;
            int vendorMatches = -1;
            int remainder, digitPower, nominalIndex;
            double EFL;
            double EPD = -1.0;
            double nominalMF, reverseMF, curMF, thicknessAfter;
            string curMaterial, prevMaterial, mfPath, tempMess, vendor;
            string debugStr = "";
            List<int> indices = new List<int>();
            combination[] bestCombinations;
            catalogLens[] catalogLensArray;
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
            bool airCompensation, saveBest, reverseElements, ignoreElemCount;
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

            // Initialize best combinations (if needed)
            bestCombinations = new combination[matches];

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
                bestPath = Path.Combine(directory, bestName + "_RSLM.ZOS");
            }
            else
            {
                saveBest = false;
                bestPath = "";
            }

            // Utilize element reversal?
            reverseElements = cbxReverse.Checked;

            // Ignore number of elements when matching?
            ignoreElemCount = cbxIgnoreElements.Checked;

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
                if (token.IsCancellationRequested)
                {
                    token.ThrowIfCancellationRequested();
                }

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

            // Initialize array of catalog lenses for a single combination
            catalogLensArray = new catalogLens[nominalLenses.Count];

            // Create a temporary file to evaluate the matches
            string[] tempFolder = { dataDir, "DeleteMe.ZOS" };
            string tempPath = Path.Combine(tempFolder);

            // Get nominal MF
            nominalMF = TheSystem.MFE.CalculateMeritFunction();

            // Save variable air thicknesses
            List<int> airThicknesses = varAirThicknesses(TheLDE, lastSurf);

            // Save the system
            TheSystem.Save();

            // Remove potential DMFS from Merit Function
            removeOperand(TheSystem.MFE, ZOSAPI.Editors.MFE.MeritOperandType.DMFS);

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

            // Update progress
            TheApplication.ProgressPercent = 10;

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
                    if (ignoreElemCount)
                    {
                        // If we ignore the number of elements
                        applyCatalogSettings(TheLensCatalog, 0, EFL, EPD, eflTolerance, epdTolerance, vendors[vendorID]);
                    }
                    else
                    {
                        applyCatalogSettings(TheLensCatalog, elemCount, EFL, EPD, eflTolerance, epdTolerance, vendors[vendorID]);
                    }

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
                        if (token.IsCancellationRequested)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        // Indicator to compare the reversed-element MF value
                        reverseMF = double.PositiveInfinity;

                        // Run the lens catalog once again and for every match (it needs to be closed for the optimization tool to be opened)
                        // Feature request: can we have a field to search for a specific lens by its name?
                        TheLensCatalog = TheSystemCopy.Tools.OpenLensCatalogs();
                        if (ignoreElemCount)
                        {
                            // If we ignore the number of elements
                            applyCatalogSettings(TheLensCatalog, 0, EFL, EPD, eflTolerance, epdTolerance, vendors[vendorID]);
                        }
                        else
                        {
                            applyCatalogSettings(TheLensCatalog, elemCount, EFL, EPD, eflTolerance, epdTolerance, vendors[vendorID]);
                        }
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
                        TheSystemCopy.LDE.GetSurfaceAt(lenStart + MatchedLens.NumberOfElements).Thickness = thicknessAfter;
                        TheSystemCopy.LDE.GetSurfaceAt(lenStart + MatchedLens.NumberOfElements).ThicknessCell.SetSolveData(ThicknessSolve);

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
                        if (vendors.Length > 0 && vendorMatches > 1)
                        {
                            TheApplication.ProgressPercent = 10.0 + nominalLensID * 50.0 / nominalLenses.Count + vendorID * 50.0 / nominalLenses.Count / vendors.Length + matchID / (vendorMatches - 1.0) * 50.0 / nominalLenses.Count / vendors.Length;
                        }
                        tempMess = "Evaluating: Lens " + (nominalLensID + 1).ToString() + "/" + nominalLenses.Count.ToString();
                        tempMess += " | Vendor " + (vendorID + 1).ToString() + "/" + (vendors.Length).ToString();
                        tempMess += " | Match " + (matchID + 1).ToString() + "/" + (vendorMatches).ToString();
                        tempMess = tempMess.PadRight(60);
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

                            // I will assume that if the MF value is zero, there has been an error when calculating the MF.
                            // It means that a perfect solution with a MF value of zero will be ignored.
                            // However, I think it is unlikely to occur with stock lenses.
                            if (curMF == 0 || reverseMF == 0)
                            {
                                mfError = true;
                            }
                            else
                            {
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
                        }

                        // Restore error flags for next match
                        materialErrorFlag = false;
                        insertionErrorFlag = false;

                        // Load the copy of the original system
                        TheSystemCopy.LoadFile(tempPath, false);
                    }
                }

                // If there are no matches at all for this nominal lens
                if (totalMatches == 0)
                {
                    // don't investigate combinations
                    combinations = false;
                }
            }

            // If combinations
            if (combinations && nominalLenses.Count > 1)
            {
                // Initialize array of best combinations
                for (int ii = 0; ii < matches; ii++)
                {
                    bestCombinations[ii] = new combination(nominalLenses.Count, null, double.PositiveInfinity);
                }

                // Enumerate combinations in base "matches". For example, if we show 5 matches and we have
                // two lenses, that is 5^2 = 25 combinations. If we enumerate those combinations in base 5
                // we get 00, 01, 02, 03, 04, 10, 11, 12, 13, 14, 20, 21, 22, 23, 24, 30, 31, 32, 33, 34,
                // 40, 41, 42, 43, 44. The first digit is the best match of the first lens, and the second
                // digit is the best match of the second lens.
                for (int ii = 0; ii < (int)Math.Pow(matches, nominalLenses.Count); ii++)
                {
                    if (token.IsCancellationRequested)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    // Update progress
                    TheApplication.ProgressPercent = 50.0 + ii / (Math.Pow(matches, nominalLenses.Count) - 1) * 50.0;
                    tempMess = "Evaluating: Combination " + (ii + 1).ToString() + "/" + ((int)Math.Pow(matches, nominalLenses.Count)).ToString();
                    TheApplication.ProgressMessage = tempMess;

                    // Clear previous indices (if any)
                    indices.Clear();

                    // This variable is redundant, but it helps me read the code
                    remainder = ii;

                    // For one combination, find indices of the best lenses matched
                    for (int jj = nominalLenses.Count - 1; jj > -1; jj--)
                    {
                        digitPower = (int)Math.Pow(matches, jj);

                        if (remainder / (digitPower * matches) > 0)
                        {
                            remainder -= (remainder / (digitPower * matches)) * digitPower * matches;
                        }

                        indices.Add(remainder / digitPower);
                    }

                    // Create the combination file
                    TheLensCatalog = TheSystemCopy.Tools.OpenLensCatalogs();

                    nominalIndex = 0;

                    foreach (int index in indices)
                    {
                        // Nominal lens properties
                        elemCount = nominalLenses[nominalIndex].ElemCount;
                        lenStart = nominalLenses[nominalIndex].StartSurf;
                        EFL = nominalLenses[nominalIndex].EFL;
                        EPD = nominalLenses[nominalIndex].EPD;

                        // Update combination array
                        catalogLensArray[nominalIndex] = bestMatches[nominalIndex, index];

                        // Best matching lens properties
                        vendor = bestMatches[nominalIndex, index].Vendor;

                        if (vendor == "")
                        {
                            missingMatch = true;
                            break;
                        }

                        // Apply catalog settings to retrieve the corresponding best match
                        if (ignoreElemCount)
                        {
                            // If we ignore the number of elements
                            applyCatalogSettings(TheLensCatalog, 0, EFL, EPD, eflTolerance, epdTolerance, vendor);
                        }
                        else
                        {
                            applyCatalogSettings(TheLensCatalog, elemCount, EFL, EPD, eflTolerance, epdTolerance, vendor);
                        }

                        TheLensCatalog.RunAndWaitForCompletion();

                        // Retrieve the corresponding matched lens
                        MatchedLens = TheLensCatalog.GetResult(bestMatches[nominalIndex, index].MatchID);

                        // Save the air thickness before next lens
                        thicknessAfter = TheSystemCopy.LDE.GetSurfaceAt(lenStart + elemCount).Thickness;
                        ThicknessSolve = TheSystemCopy.LDE.GetSurfaceAt(lenStart + elemCount).ThicknessCell.GetSolveData();

                        // Remove ideal lens
                        TheSystemCopy.LDE.RemoveSurfacesAt(lenStart, elemCount + 1);

                        // Insert matching lens
                        MatchedLens.InsertLensSeq(lenStart, ignoreObject, reverseGeometry);

                        // Reverse matching lens (if necessary)
                        if (bestMatches[nominalIndex, index].IsReversed)
                        {
                            // Reverse the matched lens
                            TheLDECopy.RunTool_ReverseElements(lenStart, lenStart + elemCount);
                        }

                        // Restore thickness
                        TheSystemCopy.LDE.GetSurfaceAt(lenStart + MatchedLens.NumberOfElements).Thickness = thicknessAfter;
                        TheSystemCopy.LDE.GetSurfaceAt(lenStart + MatchedLens.NumberOfElements).ThicknessCell.SetSolveData(ThicknessSolve);

                        nominalIndex++;
                    }

                    TheLensCatalog.Close();

                    if (missingMatch)
                    {
                        continue;
                    }

                    // Load MF
                    TheSystemCopy.MFE.LoadMeritFunction(mfPath);

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

                        TheOptimizer.Close();
                    }
                    else
                    {
                        // Retrieve the MF value
                        curMF = TheSystemCopy.MFE.CalculateMeritFunction();
                    }

                    // If the MF value is zero, assume the MF couldn't be calculated (error) and ignore this combination
                    if (curMF == 0)
                    {
                        continue;
                    }

                    isBestCombination(TheSystemCopy, saveBest, bestPath, bestCombinations, catalogLensArray, curMF);

                    // Load the copy of the original system
                    TheSystemCopy.LoadFile(tempPath, false);
                }
            }

            // Delete temporary files
            File.Delete(tempPath);
            File.Delete(tempPath.Replace(".ZOS", ".ZMX"));
            File.Delete(tempPath.Replace(".ZOS", ".ZDA"));
            File.Delete(mfPath);

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
            line = "File:".PadRight(10) + TheSystem.SystemFile;
            lines.Add(line);
            line = "Title:".PadRight(10) + TheSystem.SystemData.TitleNotes.Title;
            lines.Add(line);
            line = "Date:".PadRight(10) + DateTime.Now + "\r\n\r\n";
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
                    line = "Surfaces:".PadRight(22) + "All";
                    break;
                case VARIABLE:
                    line = "Surfaces:".PadRight(22) + "Variables";
                    break;
            }
            lines.Add(line);

            line = "Vendors:".PadRight(22);
            if (cbxVendors.Checked)
            {
                line += "All";
            }
            else
            {
                string lastVendor = vendors.Last();
                foreach (string vendorDisplay in vendors)
                {
                    if (vendorDisplay.Equals(lastVendor))
                    {
                        line += vendorDisplay;
                    }
                    else
                    {
                        line += vendorDisplay + ", ";
                    }
                }
            }
            lines.Add(line);

            line = "Show Matches:".PadRight(22) + matches.ToString();
            lines.Add(line);
            line = "EFL Tolerance (%):".PadRight(22) + eflTolerance.ToString();
            lines.Add(line);
            line = "EPD Tolerance (%):".PadRight(22) + epdTolerance.ToString();
            lines.Add(line);
            line = "Nominal Criterion:".PadRight(22) + nominalMF + "\r\n";
            lines.Add(line);

            line = "Air Thickness Compensation:".PadRight(31) + airCompensation.ToString();
            lines.Add(line);
            if (airCompensation)
            {
                line = "Optimization Cycles:".PadRight(31);
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
            line = "Save Best:".PadRight(31) + saveBest.ToString();
            lines.Add(line);
            line = "Both orientations:".PadRight(31) + reverseElements.ToString() + "\r\n";
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

            if (insertionError)
            {
                line = "> WARNING: One or more match failed to insert in the system ... \r\n";
                lines.Add(line);
            }

            if (materialError)
            {
                line = "> WARNING: One or more match had a Material incompatible with the current wavelengths defined in the system ... \r\n";
                lines.Add(line);
            }

            if (mfError)
            {
                line = "> WARNING: The Merit Function could not be evaluated for one or more match ... \r\n";
                lines.Add(line);
            }

            for (int ii = 0; ii < nominalLenses.Count; ii++)
            {
                line = "Component " + (ii + 1).ToString();
                line += " (Surfaces " + nominalLenses[ii].StartSurf.ToString() + "-";
                line += (nominalLenses[ii].StartSurf + nominalLenses[ii].ElemCount).ToString() + ")";
                line = line.PadRight(56);
                line += "MF Value".PadRight(24) + "MF Change".PadRight(24) + "Is Reversed?";
                lines.Add(line);

                for (int jj = 0; jj < matches; jj++)
                {
                    if (bestMatches[ii, jj].Vendor != "")
                    {
                        line = (jj + 1).ToString() + ") ";
                        line += (bestMatches[ii, jj].Name + "(" + bestMatches[ii, jj].Vendor + ")").PadRight(50);
                        line += "\t" + bestMatches[ii, jj].MatchedMF.ToString().PadRight(24) + "";
                        line += Math.Abs(nominalMF - bestMatches[ii, jj].MatchedMF).ToString().PadRight(24);
                        line += bestMatches[ii, jj].IsReversed;
                        lines.Add(line);
                    }
                }

                lines.Add("");
            }

            if (combinations && nominalLenses.Count > 1)
            {
                line = "Best combinations\t\t\t\t\tMF Value";
                lines.Add(line);

                int combIndex = 0;
                foreach (combination combinationResult in bestCombinations)
                {
                    if (combinationResult.CombinedMF < double.PositiveInfinity)
                    {
                        line = (combIndex + 1).ToString() + ")";

                        int lensIndex = 0;
                        foreach (catalogLens combLens in combinationResult.Lenses)
                        {
                            if (lensIndex == 0)
                            {
                                line += " " + (lensIndex + 1).ToString() + ": " + combLens.Name;
                                line += " (" + combLens.Vendor + ")";
                            }
                            else if (lensIndex == nominalLenses.Count - 1)
                            {
                                line += "   " + (lensIndex + 1).ToString() + ": ";
                                line += (combLens.Name + " (" + combLens.Vendor + ")").PadRight(50);
                                line += combinationResult.CombinedMF.ToString();
                            }
                            else
                            {
                                line += "   " + (lensIndex + 1).ToString() + ": " + combLens.Name;
                                line += " (" + combLens.Vendor + ")";
                            }

                            line += "\r\n";

                            lensIndex++;
                        }

                        lines.Add(line);
                    }

                    combIndex++;
                }
            }

            lines.Add(debugStr);

            // Write lines to text file
            File.WriteAllLines(logPath, lines.ToArray());

            // Update progress
            TheApplication.ProgressPercent = 100;
            tempMess = "Matching Complete";
            TheApplication.ProgressMessage = tempMess;
        }

        static void removeOperand(IMeritFunctionEditor MFE, ZOSAPI.Editors.MFE.MeritOperandType OpType)
        {
            int numOp = MFE.NumberOfOperands + 1;

            for (int ii = 1; ii < numOp; ii++)
            {
                if (MFE.GetOperandAt(ii).Type == OpType)
                {
                    MFE.RemoveOperandAt(ii);
                    numOp--;
                }
            }
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
            // Scale tolerances
            eflTolerance /= 100;
            epdTolerance /= 100;

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

        static void isBestCombination(IOpticalSystem TheSystemCopy, bool saveBest, string bestPath, combination[] bestCombinations, catalogLens[] combinationArray, double curMF)
        {
            int matches = bestCombinations.Length;

            for (int ii = 0; ii < matches; ii++)
            {
                if (curMF < bestCombinations[ii].CombinedMF)
                {
                    // Save if best
                    if (ii == 0 && saveBest)
                    {
                        TheSystemCopy.SaveAs(bestPath);
                    }

                    // Offset previous results
                    for (int jj = matches - 1; jj - ii > 0; jj--)
                    {
                        for (int kk = 0; kk < bestCombinations[ii].LenCount; kk++)
                        {
                            bestCombinations[jj].Lenses[kk] = bestCombinations[jj - 1].Lenses[kk];
                        }

                        bestCombinations[jj].CombinedMF = bestCombinations[jj - 1].CombinedMF;
                    }

                    // Save new match as best
                    for (int jj = 0; jj < bestCombinations[ii].LenCount; jj++)
                    {
                        bestCombinations[ii].Lenses[jj] = combinationArray[jj];
                    }
                    bestCombinations[ii].CombinedMF = curMF;

                    break;
                }
            }
        }

        private void btnTerminate_Click(object sender, EventArgs e)
        {
            _tokenSource.Cancel();
            btnTerminate.Enabled = false;
        }
    }

    public class combination
    {
        public combination(int lenCount, catalogLens[] lenses, double combinedMF)
        {
            LenCount = lenCount;
            if (lenses == null)
            {
                Lenses = new catalogLens[lenCount];
                for (int ii = 0; ii < lenCount; ii++)
                {
                    Lenses[ii] = new catalogLens(-1, -1, false, -1.0, "", "");
                }
            }
            else
            {
                Lenses = lenses;
            }
            CombinedMF = combinedMF;
        }

        public int LenCount;
        public catalogLens[] Lenses;
        public double CombinedMF;
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
