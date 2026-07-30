using Mcs.Usb;
using MCS_Devices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GUI
{
    public partial class StimulatorForm : Form
    {
        MeaDacq mea;
        public Stimulator stg { get; }
    
        ElecIDsManager elecManager;
        MeaButtonMatrix buttonsMatrix;

        Color[] STG_colors = new Color[2];

        bool STG_1_enabled = false;
        bool STG_2_enabled = false;

        int STG_selected;

        public StimulatorForm(Control Parent, MeaDacq meaDaq)
        {
            InitializeComponent();

            mea = meaDaq;
            stg = new Stimulator(mea.Get_MeaLayout());

            elecManager = new ElecIDsManager(mea.Get_MeaLayout());

            // Stim elecs matrix:
            int elecSide = 40;
            int left_x = 200;
            int bottom_y = 25;
            buttonsMatrix = new MeaButtonMatrix(mea.Get_MeaLayout(), Controls, Electrode_ButtonClick, elecSide, left_x, bottom_y);

            // Set_GUI_Parameters();
            Set_CanvasSize();
            FormBorderStyle = FormBorderStyle.None;
            //Size = Parent.Size;
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;

            // Disconnect all electrodes from Stimulator
            stg.Connect_USB_A();
            stg.Deactivate_Full_MEA();

            STG_colors[0] = Color.LightSeaGreen;
            STG_colors[1] = Color.Crimson;

            // STG 1 by default:
            Select_STG(1);
            STG_1_enabled = true;
            stg.Set_Stimulation_Triggers(STG_1_enabled, STG_2_enabled);
        }
            

        private void Set_CanvasSize()
        {
            int[] size = buttonsMatrix.Get_buttons_size();
            Size = new Size(size[0] + 150, size[1] + 175);
        }



        private void Electrode_ButtonClick(object sender, EventArgs e)
        {
            Button clickedButton = (Button)sender;
            int elec_ind = Convert.ToInt32(clickedButton.Name); // index in the matrix of electrodes
            int elec_ID = elecManager.GetIDFromIndex(elec_ind); // electrode hardware ID

            if (buttonsMatrix.Get_SelectedState(clickedButton))
            {
                // Deactivate electrode:
                buttonsMatrix.UnselectButton(elec_ind);
                stg.Remove_STG_StimElec_ID(elec_ID, STG_selected);
                stg.Deactivate_StimElecID(elec_ID);
            }
            else
            {
                if (STG_1_enabled || STG_2_enabled)
                {
                    // Activate electrode:
                    buttonsMatrix.SelectButton(elec_ind);
                    buttonsMatrix.ChangeButtonColors(elec_ind, STG_colors[STG_selected - 1]);
                    stg.Add_STG_StimElec_ID(elec_ID, STG_selected);
                    stg.Activate_StimElecID(elec_ID, STG_selected);
                }
            }
        }

        private void download_button_Click(object sender, EventArgs e)
        {
            int[] amplitude_mV = new int[] { int.Parse(Amp1_text.Text), int.Parse(Amp2_text.Text) };
            ulong[] duration_us = new ulong[] { ulong.Parse(duration1_Text.Text), ulong.Parse(duration2_Text.Text) };
            stg.DownloadStimulus(STG_selected, amplitude_mV, duration_us);

            // Enable stimulation button:
            stimulate_Button.Enabled = true;
        }

        private (int, ulong) Get_Stimulation_Parameters()
        {
            int amplitude_mV = int.Parse(Amp1_text.Text);
            ulong duration_us = ulong.Parse(duration1_Text.Text);

            return (amplitude_mV, duration_us);
        }

        private void stimulate__Button_Click(object sender, EventArgs e)
        {
           if (STG_1_enabled || STG_2_enabled)
            stg.Stimulate();
        }


        private void STG_1_button_Click(object sender, EventArgs e)
        {            
            if (STG_1_enabled)
            {
                STG_1_enabled = false;
                STG_1_button.BackColor = Color.LightGray;
                                
                // If STG 2 was active, select STG 2
                if (STG_2_enabled)
                    Select_STG(2);
                // If STG 2 was not active, disable download button
                else download_Button.BackColor = Color.LightGray;
            }
            else
            {
                Select_STG(1);               
            }
            stg.Set_Stimulation_Triggers(STG_1_enabled, STG_2_enabled);
            ManageStimulateButton(STG_1_enabled, STG_2_enabled);
        }


        private void STG_2_button_Click(object sender, EventArgs e)
        {
            if (STG_2_enabled)
            {
                STG_2_enabled = false;
                STG_2_button.BackColor = Color.LightGray;

                // If STG 1 was active, select STG 2
                if (STG_1_enabled) 
                    Select_STG(1);
                // If STG 1 was not active, disable download button
                else download_Button.BackColor = Color.LightGray;                              
            }
            else
            {
                Select_STG(2);
            }

            stg.Set_Stimulation_Triggers(STG_1_enabled, STG_2_enabled);
            ManageStimulateButton(STG_1_enabled, STG_2_enabled);
        }

        public void ManageStimulateButton(bool STG_1_enabled, bool STG_2_enabled)
        {
            bool STG_1_ok = STG_1_enabled && stg.Get_STG_1_downloaded();
            bool STG_2_ok = STG_2_enabled && stg.Get_STG_2_downloaded();

            if (STG_1_ok || STG_2_ok)
                stimulate_Button.Enabled = true;

            else
                stimulate_Button.Enabled = false;
        }


        public void Select_STG(int STG_ID)
        {       
            STG_selected = STG_ID;
            download_Button.BackColor = STG_colors[STG_ID-1];

            if (STG_ID == 1)
            {
                STG_1_enabled = true;
                STG_1_button.BackColor = STG_colors[0]; 
            }
            else
            {
                STG_2_enabled = true;
                STG_2_button.BackColor = STG_colors[1];   
            }         
        }

        private void StimulatorForm_Load(object sender, EventArgs e)
        {

        }
   
    }
}
