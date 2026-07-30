using MCS_Devices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;

namespace GUI
{
    /// <summary>
    /// Class to create matrix of clickable electrodes in the GUI
    /// Each electrode is a button which can has 2 states - locked or unlocked
    /// </summary>
    public class MeaButtonMatrix
    {
        Button[] allButtons;
        //  CheckBox[] allChecks;

        // Button states:
        bool[] selected;// whether the button (electrode) is currently selected
        bool[] enabled; // whether the button (electrode) is clikable or not

        Color selectedColor;
        Color unselectedColor;


        //----------------------------------------------------------------------------------------//
        int elecSide;

        MeaElecCoords elecCoordsManager;
        ElecIDsManager elecIDsManager;
 //      int[] elec_IDs;
        int[,] buttonCoords;
        Control.ControlCollection controls;
        //----------------------------------------------------------------------------------------//


        public MeaButtonMatrix(MeaLayoutEnum meaLayout, Control.ControlCollection controls, EventHandler BtnClick = null, int elecSide = 30, 
            int left_x = 15, int bottom_y = 90, int wellSpacing = 15, Color? selectedcolor = null, Color? unselectedcolor = null,  EventHandler BoxClick = null)
        {
            this.controls = controls;
            this.elecSide = elecSide;
            
            // Electrodes coordinates:
            elecCoordsManager = new MeaElecCoords(meaLayout, left_x, bottom_y, elecSide, elecSide, wellSpacing);
            buttonCoords = elecCoordsManager.GetElecCoords();

            // Electrodes IDs:
            elecIDsManager = new ElecIDsManager(meaLayout);

            // Button State Colors:
            selectedColor = selectedcolor ?? Color.LightSeaGreen; 
            unselectedColor = unselectedcolor ?? Color.White;       

            allButtons = CreateElecButtons(BtnClick);
        }


        private Button[] CreateElecButtons(EventHandler Btn_Click)
        {
            allButtons = new Button[elecCoordsManager.Get_nElecs()];
            var elecLabels = elecIDsManager.GetAllLabels();
            int nElecs = elecCoordsManager.Get_nElecs();

            enabled = new bool[nElecs];
            selected = new bool[nElecs];

            for (int elec_i = 0; elec_i < nElecs; elec_i++)
            {
                Button btn = new Button();

                btn.Left = buttonCoords[0, elec_i];
                btn.Top = buttonCoords[1, elec_i];
                btn.Width = elecSide;
                btn.Height = elecSide;
                btn.Name = elec_i.ToString();
                btn.Text = elecLabels[elec_i];
                btn.BackColor = unselectedColor;
                btn.Font = new Font(btn.Font.FontFamily, 7);
                btn.Visible = true;

                btn.Click += Btn_Click;
                allButtons[elec_i] = btn;
                controls.Add(allButtons[elec_i]);

                // Buttons states:
                enabled[elec_i] = true;
                selected[elec_i] = false;
            }
            return allButtons;
        }


        public void DisableAllButtons() 
        {
            for (int i = 0; i < allButtons.Length; i++)
            {
                allButtons[i].Enabled = false;
                enabled[i] = false;
            }            
        }


        public void EnableAllButtons()
        {
            for (int i = 0; i < allButtons.Length; i++)
            {
                allButtons[i].Enabled = true;
                enabled[i] = true;
            }
        }


        public void DisableAllButtons(int[] elec_inds)
        {
            for (int i = 0; i < elec_inds.Length; i++)
            {
                allButtons[elec_inds[i]].Enabled = false;
                enabled[elec_inds[i]] = false;
            }
        }        


        public void EnableButton(int[] elec_inds)
        {
            for (int i = 0; i < elec_inds.Length; i++)
            {
                allButtons[elec_inds[i]].Enabled = true;
                enabled[elec_inds[i]] = true;
            }
        }


        public void DisableButton(int elec_ind)
        {
            allButtons[elec_ind].Enabled = false;
            enabled[elec_ind] = false;
        }


        public void EnableButton(int elec_ind)
        {
            allButtons[elec_ind].Enabled = true;
            enabled[elec_ind] = true;
        }


        public void SelectButton(int elec_ind)
        {
            allButtons[elec_ind].BackColor = selectedColor;
            selected[elec_ind] = true;
        }


        public void SelectButtons(int[] elec_inds) 
        {
            for (int i = 0; i < elec_inds.Length; i++)
            {
                allButtons[elec_inds[i]].BackColor = selectedColor;
                selected[elec_inds[i]] = true;
            }
        }


        public void UnselectButton(int elec_ind)
        {
            allButtons[elec_ind].BackColor = unselectedColor;
            selected[elec_ind] = false;
        }


        public void UnselectedButtons(int[] elec_inds) 
        {
            for (int i = 0; i < elec_inds.Length; i++)
            {
                allButtons[elec_inds[i]].BackColor = unselectedColor;
                selected[elec_inds[i]] = false;
            }
        }


        public void ChangeAllButtonsColors(Color color)
        {
            int nelecs = allButtons.Length;
            for (int i = 0; i < nelecs; i++)
            {
                allButtons[i].BackColor = color;
            }
        }


        public void ChangeButtonsColors(int[] elec_inds, Color color)
        {
            int nelecs = elec_inds.Length;
            for (int i = 0; i< nelecs; i++)
            {
                allButtons[elec_inds[i]].BackColor = color;
            }
        }


        public void ChangeButtonColors(int elec_ind, Color color)
        {      
                allButtons[elec_ind].BackColor = color;
        }

        /*
        private CheckBox[] CreateCheckBoxes(EventHandler Box_CheckedChanged, Control.ControlCollection Controls)
        {

            int ind = 0;
            allChecks = new CheckBox[nLines + nCols];
            string letters = "ABCDEFGHJKLMNOPR";

            for (int line = 0; line < nLines; line++)
            {
                CheckBox box = new CheckBox();

                box.Left = x_corner - 20;
                box.Top = line * sides[0] + y_corner;
                box.Width = sides[0];
                box.Height = sides[1];
                if (line < 9)
                    box.Name = "num_0" + (line + 1).ToString();
                else
                    box.Name = "num_" + (line + 1).ToString();

                // box.Checked = true;
                box.Checked = false;
                box.CheckedChanged += Box_CheckedChanged;

                allChecks[ind] = box;
                Controls.Add(allChecks[ind]);
                ind++;
            }

            for (int col = 0; col < nCols; col++)
            {
                CheckBox box = new CheckBox();

                box.Left = col * sides[1] + x_corner + 12;
                box.Top = (nLines) * sides[1] + y_corner + 5;
                box.Width = sides[0];
                box.Height = sides[1];
                box.Checked = false;
                box.CheckedChanged += Box_CheckedChanged;
                box.Name = "col_" + letters[col];
                allChecks[ind] = box;
                Controls.Add(allChecks[ind]);
                ind++;
            }

            return allChecks;
        }

        public CheckBox[] Get_allChecks()
        {
            return allChecks;
        }

        */

        public Button[] Get_allButtons()
        {
            return allButtons;
        }
        
        public bool Get_SelectedState(int ind)
        {
            return selected[ind];
        }

        public bool Get_SelectedState(Button button)
        {
            int ind = int.Parse(button.Name);
            return selected[ind];
        }

        public bool[] Get_allSelectedStates()
        {
            return selected;
        }

        public bool[] Get_allEnabledStates()
        {
            return enabled;
        }

        public bool Get_EnabledState(Button button)
        {
            return button.Enabled;
        }


        public Color Get_SelectedColor()
        {
            return selectedColor;
        }

        public Color Get_UnselectedColor()
        {
            return unselectedColor;
        }

        public int[] Get_buttons_size()
        {
            int max_x = 0;
            int max_y = 0;
            for (int i = 0; i < allButtons.Length; i++)
            {
                if (allButtons[i].Location.X > max_x)
                    max_x = allButtons[i].Location.X;

                if (allButtons[i].Location.Y > max_y)
                    max_y = allButtons[i].Location.Y;
            }
            int[] size = new int[2] { max_x, max_y };
            return size;
        }
    }
}
