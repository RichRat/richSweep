﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace richSweep
{
    interface ISolver
    {
        void SolveStep (bool first);
        void Failed();
    }
}