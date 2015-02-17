using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace richSweep
{
    /// <summary>
    /// Masks Functions for Solver
    /// </summary>
    public interface IRestrictedField : IEnumerable<IRestrictedField>, IEnumerator<IRestrictedField>
    {
        Field.Mode FieldMode { get; }
        void RightClick();
        void ClickArea();
        void Click();
        int RValue { get; }
        int X { get; }
        int Y { get; }
    }
}
