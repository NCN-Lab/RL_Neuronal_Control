using MCS_Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;

namespace GUI
{
    internal class MeaElecCoords
    {
        int bottomLeft_x;
        int bottomLeft_y;
        int elecWidth;
        int elecHeight;
        int wellSpacing;
        int nElecs;
        int nWells;
        int nWellRows;
        bool withCornerElecs;
        int nLines;
        int nCols;
        int[] wellPosition;

        MeaLayoutEnum meaLayout;
        int[,] elecCoords;

        public MeaElecCoords(MeaLayoutEnum mea_layout, int buttonLeft_x, int bottomLeft_y, int elecWidth = 15, int elecHight = 15, int wellSpacing = 15)
        {
            this.bottomLeft_x = buttonLeft_x;
            this.bottomLeft_y = bottomLeft_y;
            this.elecWidth = elecWidth;
            this.elecHeight = elecHight; 
            this.wellSpacing = wellSpacing;
            SetMeaLayoutParams(mea_layout);
            elecCoords = new int[nElecs, nElecs];
            CalculateElecCoords();
        }


        private void SetMeaLayoutParams(MeaLayoutEnum mea_layout)
        {
            switch (mea_layout)
            {
                case MeaLayoutEnum.MEA256_1well:
                    nElecs = 252;
                    nWells = 1;
                    nWellRows = 1;
                    nLines = 16;
                    nCols = 16;
                    wellPosition = new int[] { 0 };
                    withCornerElecs = false;
                    break;

                case MeaLayoutEnum.MEA256_6well:
                    nElecs = 252;
                    nWells = 6;
                    nWellRows = 2;
                    nLines = 6;
                    nCols = 7;
                    wellPosition = new int[] { 2,4,5,3,1,0 }; // FCADBF - column wise
                    withCornerElecs = true;
                    break;

                case MeaLayoutEnum.MEA256_9well:
                    nElecs = 234;
                    nWells = 9;
                    nWellRows = 3;
                    nLines = 5;
                    nCols = 6;
                    wellPosition = new int[] {0,3,6,1,4,7,2,5,8 }; // ABCDEFGHJ - column wise
                    withCornerElecs = false;
                    break;

                case MeaLayoutEnum.MEA60_1well:
                    nElecs = 60;
                    nWells = 1;
                    nWellRows = 1;
                    nLines = 8;
                    nCols = 8;
                    wellPosition = new int[] { 0 }; 
                    withCornerElecs = false;                                                   
                    break;

                case MeaLayoutEnum.MEA60_6well:
                    nElecs = 54;
                    nWells = 6;
                    nWellRows = 2;
                    nLines = 3;
                    nCols = 3;
                    wellPosition = new int[] { 2, 4, 5, 3, 1, 0 }; // FCADBF - column wise
                    withCornerElecs = true;
                    break;
            }
        }

        private void CalculateElecCoords()
        {
            int elec = 0;
            int nWellCols = nWells / nWellRows;
            int wellWidth = nCols * elecWidth + wellSpacing;
            int wellHight = nLines * elecHeight + wellSpacing;

            for (int well_i = 0; well_i < nWells; well_i++)
            {
                int well = wellPosition[well_i];
                for (int col = 0; col < nCols; col++)
                {
                    for (int line = 0; line < nLines; line++)
                    {
                        bool atCorner = false;
                        if ((line == 0 && col == 0) || (line == nLines-1 && col == 0) || (line == 0 && col == nCols-1) || (line == nLines-1 && col == nCols-1))
                                atCorner = true;

                        if (!atCorner || withCornerElecs)
                        {
                            elecCoords[0, elec] = bottomLeft_x + col * elecWidth + (int)Math.Floor((decimal)well / nWellRows) * wellWidth;
                            elecCoords[1, elec] = bottomLeft_y + line * elecHeight + well % nWellRows * wellHight;
                            elec++;
                        }
                    }
                }
            }
        }

        public int[,] GetElecCoords()
        {
            return elecCoords;
        }

        public int Get_nElecs()
        {
            return nElecs;
        }

        public int Get_nLines()
        {
            return nLines;
        }

        public int Get_nCols() 
        {
            return nCols;
        }

    }
}
