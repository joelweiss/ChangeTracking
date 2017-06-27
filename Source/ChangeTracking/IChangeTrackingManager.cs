using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChangeTracking
{
    internal interface IChangeTrackingManager
    {
        bool Delete();
        bool UnDelete();
        void UpdateStatus();
        void SetAdded();
    }
}