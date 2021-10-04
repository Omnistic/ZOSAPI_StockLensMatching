
namespace Reverse_SLM
{
    partial class Settings
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.lbxVendors = new System.Windows.Forms.ListBox();
            this.label2 = new System.Windows.Forms.Label();
            this.cbxVendors = new System.Windows.Forms.CheckBox();
            this.rbtnSurfacesVariable = new System.Windows.Forms.RadioButton();
            this.rbtnSurfacesAll = new System.Windows.Forms.RadioButton();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label3 = new System.Windows.Forms.Label();
            this.numMatches = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.tbxEFL = new System.Windows.Forms.TextBox();
            this.tbxEPD = new System.Windows.Forms.TextBox();
            this.cbxAirThicknessCompensation = new System.Windows.Forms.CheckBox();
            this.cbxSaveBest = new System.Windows.Forms.CheckBox();
            this.comboCycles = new System.Windows.Forms.ComboBox();
            this.cbxReverse = new System.Windows.Forms.CheckBox();
            this.btnLaunch = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnTerminate = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMatches)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(27, 29);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(113, 29);
            this.label1.TabIndex = 2;
            this.label1.Text = "Surfaces:";
            // 
            // lbxVendors
            // 
            this.lbxVendors.Enabled = false;
            this.lbxVendors.FormattingEnabled = true;
            this.lbxVendors.ItemHeight = 29;
            this.lbxVendors.Items.AddRange(new object[] {
            "Item 1",
            "Item 2"});
            this.lbxVendors.Location = new System.Drawing.Point(288, 149);
            this.lbxVendors.Name = "lbxVendors";
            this.lbxVendors.SelectionMode = System.Windows.Forms.SelectionMode.MultiSimple;
            this.lbxVendors.Size = new System.Drawing.Size(480, 236);
            this.lbxVendors.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(27, 90);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(125, 29);
            this.label2.TabIndex = 4;
            this.label2.Text = "Vendor(s):";
            // 
            // cbxVendors
            // 
            this.cbxVendors.AutoSize = true;
            this.cbxVendors.Checked = true;
            this.cbxVendors.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbxVendors.Location = new System.Drawing.Point(288, 89);
            this.cbxVendors.Name = "cbxVendors";
            this.cbxVendors.Size = new System.Drawing.Size(84, 33);
            this.cbxVendors.TabIndex = 5;
            this.cbxVendors.Text = "All?";
            this.cbxVendors.UseVisualStyleBackColor = true;
            this.cbxVendors.CheckedChanged += new System.EventHandler(this.cbxVendors_CheckedChanged);
            // 
            // rbtnSurfacesVariable
            // 
            this.rbtnSurfacesVariable.AutoSize = true;
            this.rbtnSurfacesVariable.Location = new System.Drawing.Point(121, 3);
            this.rbtnSurfacesVariable.Name = "rbtnSurfacesVariable";
            this.rbtnSurfacesVariable.Size = new System.Drawing.Size(133, 33);
            this.rbtnSurfacesVariable.TabIndex = 0;
            this.rbtnSurfacesVariable.Text = "Variable";
            this.rbtnSurfacesVariable.UseVisualStyleBackColor = true;
            // 
            // rbtnSurfacesAll
            // 
            this.rbtnSurfacesAll.AutoSize = true;
            this.rbtnSurfacesAll.Checked = true;
            this.rbtnSurfacesAll.Location = new System.Drawing.Point(3, 3);
            this.rbtnSurfacesAll.Name = "rbtnSurfacesAll";
            this.rbtnSurfacesAll.Size = new System.Drawing.Size(71, 33);
            this.rbtnSurfacesAll.TabIndex = 7;
            this.rbtnSurfacesAll.TabStop = true;
            this.rbtnSurfacesAll.Text = "All";
            this.rbtnSurfacesAll.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.rbtnSurfacesVariable);
            this.panel1.Controls.Add(this.rbtnSurfacesAll);
            this.panel1.Location = new System.Drawing.Point(288, 24);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(310, 38);
            this.panel1.TabIndex = 7;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(27, 423);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(176, 29);
            this.label3.TabIndex = 8;
            this.label3.Text = "Show matches:";
            // 
            // numMatches
            // 
            this.numMatches.Location = new System.Drawing.Point(288, 421);
            this.numMatches.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.numMatches.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numMatches.Name = "numMatches";
            this.numMatches.Size = new System.Drawing.Size(268, 35);
            this.numMatches.TabIndex = 9;
            this.numMatches.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numMatches.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.numMatches_KeyPress);
            this.numMatches.Leave += new System.EventHandler(this.numMatches_Leave);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(27, 489);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(223, 29);
            this.label4.TabIndex = 10;
            this.label4.Text = "EFL Tolerance (%);";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(27, 549);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(228, 29);
            this.label5.TabIndex = 11;
            this.label5.Text = "EPD Tolerance (%):";
            // 
            // tbxEFL
            // 
            this.tbxEFL.Location = new System.Drawing.Point(288, 486);
            this.tbxEFL.MaxLength = 4;
            this.tbxEFL.Name = "tbxEFL";
            this.tbxEFL.Size = new System.Drawing.Size(268, 35);
            this.tbxEFL.TabIndex = 12;
            this.tbxEFL.Text = "25";
            this.tbxEFL.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tolerance_char_validation);
            this.tbxEFL.Leave += new System.EventHandler(this.tolerance_max_validation);
            // 
            // tbxEPD
            // 
            this.tbxEPD.Location = new System.Drawing.Point(288, 546);
            this.tbxEPD.Name = "tbxEPD";
            this.tbxEPD.Size = new System.Drawing.Size(268, 35);
            this.tbxEPD.TabIndex = 13;
            this.tbxEPD.Text = "25";
            this.tbxEPD.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tolerance_char_validation);
            this.tbxEPD.Leave += new System.EventHandler(this.tolerance_max_validation);
            // 
            // cbxAirThicknessCompensation
            // 
            this.cbxAirThicknessCompensation.AutoSize = true;
            this.cbxAirThicknessCompensation.Location = new System.Drawing.Point(288, 614);
            this.cbxAirThicknessCompensation.Name = "cbxAirThicknessCompensation";
            this.cbxAirThicknessCompensation.Size = new System.Drawing.Size(364, 33);
            this.cbxAirThicknessCompensation.TabIndex = 14;
            this.cbxAirThicknessCompensation.Text = "Air Thickness Compensation?";
            this.cbxAirThicknessCompensation.UseVisualStyleBackColor = true;
            this.cbxAirThicknessCompensation.CheckedChanged += new System.EventHandler(this.cbxAirThicknessCompensation_CheckedChanged);
            // 
            // cbxSaveBest
            // 
            this.cbxSaveBest.AutoSize = true;
            this.cbxSaveBest.Location = new System.Drawing.Point(288, 742);
            this.cbxSaveBest.Name = "cbxSaveBest";
            this.cbxSaveBest.Size = new System.Drawing.Size(307, 33);
            this.cbxSaveBest.TabIndex = 15;
            this.cbxSaveBest.Text = "Save Best Combination?";
            this.cbxSaveBest.UseVisualStyleBackColor = true;
            // 
            // comboCycles
            // 
            this.comboCycles.Enabled = false;
            this.comboCycles.FormattingEnabled = true;
            this.comboCycles.Items.AddRange(new object[] {
            "Automatic",
            "1",
            "5",
            "10",
            "50"});
            this.comboCycles.Location = new System.Drawing.Point(288, 671);
            this.comboCycles.Name = "comboCycles";
            this.comboCycles.Size = new System.Drawing.Size(268, 37);
            this.comboCycles.TabIndex = 16;
            // 
            // cbxReverse
            // 
            this.cbxReverse.AutoSize = true;
            this.cbxReverse.Location = new System.Drawing.Point(288, 800);
            this.cbxReverse.Name = "cbxReverse";
            this.cbxReverse.Size = new System.Drawing.Size(276, 33);
            this.cbxReverse.TabIndex = 17;
            this.cbxReverse.Text = "Try both orientations?";
            this.cbxReverse.UseVisualStyleBackColor = true;
            // 
            // btnLaunch
            // 
            this.btnLaunch.Location = new System.Drawing.Point(32, 882);
            this.btnLaunch.Name = "btnLaunch";
            this.btnLaunch.Size = new System.Drawing.Size(223, 62);
            this.btnLaunch.TabIndex = 18;
            this.btnLaunch.Text = "Ok";
            this.btnLaunch.UseVisualStyleBackColor = true;
            this.btnLaunch.Click += new System.EventHandler(this.btnLaunch_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(288, 882);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(223, 62);
            this.btnCancel.TabIndex = 19;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnTerminate
            // 
            this.btnTerminate.Enabled = false;
            this.btnTerminate.Location = new System.Drawing.Point(545, 882);
            this.btnTerminate.Name = "btnTerminate";
            this.btnTerminate.Size = new System.Drawing.Size(223, 62);
            this.btnTerminate.TabIndex = 20;
            this.btnTerminate.Text = "Terminate";
            this.btnTerminate.UseVisualStyleBackColor = true;
            this.btnTerminate.Visible = false;
            this.btnTerminate.Click += new System.EventHandler(this.btnTerminate_Click);
            // 
            // Settings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(14F, 29F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 984);
            this.Controls.Add(this.btnTerminate);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnLaunch);
            this.Controls.Add(this.cbxReverse);
            this.Controls.Add(this.comboCycles);
            this.Controls.Add(this.cbxSaveBest);
            this.Controls.Add(this.cbxAirThicknessCompensation);
            this.Controls.Add(this.tbxEPD);
            this.Controls.Add(this.tbxEFL);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.numMatches);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.cbxVendors);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lbxVendors);
            this.Controls.Add(this.label1);
            this.Name = "Settings";
            this.Text = "Reverse SLM";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMatches)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox lbxVendors;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox cbxVendors;
        private System.Windows.Forms.RadioButton rbtnSurfacesVariable;
        private System.Windows.Forms.RadioButton rbtnSurfacesAll;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown numMatches;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox tbxEFL;
        private System.Windows.Forms.TextBox tbxEPD;
        private System.Windows.Forms.CheckBox cbxAirThicknessCompensation;
        private System.Windows.Forms.CheckBox cbxSaveBest;
        private System.Windows.Forms.ComboBox comboCycles;
        private System.Windows.Forms.CheckBox cbxReverse;
        private System.Windows.Forms.Button btnLaunch;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnTerminate;
    }
}