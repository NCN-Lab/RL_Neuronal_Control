//using General_Logic;
using MCS_Devices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GUI
{
    public partial class MonitorElecsForm : Form
    {

        //ActiveElecsManager activeElecsManager;
        MeaButtonMatrix buttonsMatrix;
        ElecIDsManager elecIDsManager;


        double t_s = 0;
        List<int> monitorElec_IDs = new List<int>();
        int[] canvas_size;

        Color monitorColor = Color.LightSeaGreen;

        public MonitorElecsForm(Control Parent, MeaDacq mea)
        {           

            elecIDsManager = new ElecIDsManager(mea.Get_MeaLayout());

            // Monitoring Electrodes:
            int elecSide = 40;
            int left_x = 80;
            int bottom_y = 80;
            buttonsMatrix = new MeaButtonMatrix(mea.Get_MeaLayout(), Controls, MonitorElec_MouseDown, elecSide, left_x, bottom_y);

            Set_CanvasSize();
            FormBorderStyle = FormBorderStyle.None;
            //Size = Parent.Size;
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;

            InitializeComponent();
        }



        private void Set_CanvasSize()
        {
            canvas_size = buttonsMatrix.Get_buttons_size();
            Size = new Size(canvas_size[0] + 150, canvas_size[1] + 175);
        }


        public int[] Get_Canvas_Size()
        {
            return canvas_size;
        }

        private void MonitorElec_MouseDown(object sender, EventArgs e)
        {
            Button clickedButton = (Button)sender;
            int elec_ind = Convert.ToInt32(clickedButton.Name); // index in the matrix of electrodes
            int elec_ID = elecIDsManager.GetIDFromIndex(elec_ind); // electrode hardware ID

            if (buttonsMatrix.Get_SelectedState(clickedButton))
            {
                // Deactivate electrode:
                buttonsMatrix.UnselectButton(elec_ind);
                monitorElec_IDs.Remove(elec_ID);
            }
            else
            {
                    // Activate electrode:
                    buttonsMatrix.SelectButton(elec_ind);
                    buttonsMatrix.ChangeButtonColors(elec_ind, monitorColor);
                    monitorElec_IDs.Add(elec_ID);
            }

        }


        public List<int> Get_MonitoringElecs()
        {
            return monitorElec_IDs;
        }
        
        private void update_button_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < buttonsMatrix.Get_allButtons().Length; i++)
            {

                // Activate electrode:
                buttonsMatrix.SelectButton(i);
                buttonsMatrix.ChangeButtonColors(i, monitorColor);
                monitorElec_IDs.Add(i);
            }
        }

        private void selectAll_check_CheckedChanged(object sender, EventArgs e)
        {
            if (selectAll_check.Checked)
            {
                for (int i = 0; i < buttonsMatrix.Get_allButtons().Length; i++)
                {

                    // Activate electrode:
                    buttonsMatrix.SelectButton(i);
                    buttonsMatrix.ChangeButtonColors(i, monitorColor);
                    monitorElec_IDs.Add(i);
                }
            }
            else
            {
                for (int i = 0; i < buttonsMatrix.Get_allButtons().Length; i++)
                {
                    // Deactivate electrode:
                    buttonsMatrix.UnselectButton(i);
                    monitorElec_IDs.Remove(i);
                }
            }
        }
    }
}
