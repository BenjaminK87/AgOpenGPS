﻿using System.Collections.Generic;

namespace AgOpenGPS
{
    public partial class CBoundary
    {
        //copy of the mainform address
        private readonly FormGPS mf;

        public List<CBoundaryList> bndList = new List<CBoundaryList>();

        public List<RatePolyline> Rate = new List<RatePolyline>();

        //constructor
        public CBoundary(FormGPS _f)
        {
            mf = _f;
            turnSelected = 0;
            isHeadlandOn = false;
            isSectionControlledByHeadland = Properties.Settings.Default.setHeadland_isSectionControlled;
        }
    }
}