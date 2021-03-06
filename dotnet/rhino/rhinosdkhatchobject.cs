#pragma warning disable 1591
using Rhino.Geometry;

#if RHINO_SDK
namespace Rhino.DocObjects
{
  public class HatchObject : RhinoObject
  {
    internal HatchObject(uint serialNumber)
      : base(serialNumber) { }

    internal override CommitGeometryChangesFunc GetCommitFunc()
    {
      return UnsafeNativeMethods.CRhinoHatch_InternalCommitChanges;
    }

    public Hatch HatchGeometry
    {
      get { return Geometry as Hatch; }
    }
  }
}
#endif