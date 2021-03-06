#pragma warning disable 1591
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Rhino.Geometry;

namespace Rhino.DocObjects
{
  /// <summary>
  /// The possible relationships between the instance definition geometry
  /// and the archive containing the original defition.
  /// </summary>
  public enum InstanceDefinitionUpdateType : int
  {
    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    // NOTE - When wrapping functions that use InstanceDefinitionUpdateType
    // make sure to talk to Steve or Dale Lear first.  The underlying enum
    // has a value that we no longer use.
    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

    /// <summary>
    /// The Rhino user interface uses the term "Embedded" for Static update types.
    /// This instance definition is never updated. If m_source_archive is set,
    /// it records the origin of the instance definition geometry, but
    /// m_source_archive is never used to update the instance definition.
    /// </summary>
    Static = 0,
    /// <summary>
    /// This instance definition geometry was imported from another archive (m_source_archive)
    /// and is embedded. If m_source_archive changes, the user is asked if they want to update
    /// the instance definition.
    /// </summary>
    [Obsolete("Always use Static")]
    Embedded = 1,
    /// <summary>
    /// This instance definition geometry was imported from another archive (m_source_archive)
    /// and is embedded. If m_source_archive changes, the instance definition is automatically
    /// updated. If m_source_archive is not available, the instance definition is still valid.
    /// </summary>
    LinkedAndEmbedded = 2,
    /// <summary>
    /// This instance definition geometry was imported from another archive (m_source_archive)
    /// and is not embedded. If m_source_archive changes, the instance definition is automatically
    /// updated. If m_source_archive is not available, the instance definition is not valid.
    /// This does not save runtime memory.  It may save a little disk space, but it is a  foolish
    /// option requested by people who do not understand all the issues.
    /// </summary>
    Linked = 3
  }

  /// <summary>
  /// A InstanceDefinitionUpdateType.Static or InstanceDefinitionUpdateType.LinkedAndEmbedded idef
  /// must have LayerStyle = Unset, a InstanceDefinitionUpdateType.Linked InstanceDefnition must
  /// have LayerStyle = Active or Reference
  /// </summary>
  public enum InstanceDefinitionLayerStyle
  {
    None = 0,
    Active = 1,   // linked InstanceDefinition layers will be active
    Reference = 2 // linked InstanceDefinition layers will be reference
  }

  /// <summary>
  /// The archive file of a linked instance definition can have the following possible states.
  /// Use InstanceObject.ArchiveFileStatus to query a instance definition's archive file status.
  /// </summary>
  public enum InstanceDefinitionArchiveFileStatus : int
  {
    /// <summary>
    /// The instance definition is not a linked instance definition.
    /// </summary>
    NotALinkedInstanceDefinition = -3,
    /// <summary>
    /// The instance definition's archive file is not readable.
    /// </summary>
    LinkedFileNotReadable = -2,
    /// <summary>
    /// The instance definition's archive file cannot be found.
    /// </summary>
    LinkedFileNotFound = -1,
    /// <summary>
    /// The instance definition's archive file is up-to-date.
    /// </summary>
    LinkedFileIsUpToDate = 0,
    /// <summary>
    /// The instance definition's archive file is newer.
    /// </summary>
    LinkedFileIsNewer = 1,
    /// <summary>
    /// The instance definition's archive file is older.
    /// </summary>
    LinkedFileIsOlder = 2,
    /// <summary>
    /// The instance definition's archive file is different.
    /// </summary>
    LinkedFileIsDifferent = 3
  }


#if RHINO_SDK
  public class InstanceObject : RhinoObject
  {
    internal InstanceObject(uint serialNumber)
      : base(serialNumber) { }

    /// <summary>
    /// transformation applied to an instance definition for this object.
    /// </summary>
    public Transform InstanceXform
    {
      get
      {
        Transform xf = new Transform();
        IntPtr ptr = ConstPointer();
        UnsafeNativeMethods.CRhinoInstanceObject_InstanceXform(ptr, ref xf);
        return xf;
      }
    }

    /// <summary>Basepoint coordinates of a block.</summary>
    /// <example>
    /// <code source='examples\vbnet\ex_blockinsertionpoint.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_blockinsertionpoint.cs' lang='cs'/>
    /// <code source='examples\py\ex_blockinsertionpoint.py' lang='py'/>
    /// </example>
    public Point3d InsertionPoint
    {
      get
      {
        Point3d rc = new Point3d(0, 0, 0);
        rc.Transform(InstanceXform);
        return rc;
      }
    }

    /// <summary>instance definition that this object uses.</summary>
    /// <example>
    /// <code source='examples\vbnet\ex_instancedefinitionobjects.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_instancedefinitionobjects.cs' lang='cs'/>
    /// <code source='examples\py\ex_instancedefinitionobjects.py' lang='py'/>
    /// </example>
    public InstanceDefinition InstanceDefinition
    {
      get
      {
        IntPtr ptr = ConstPointer();
        int docId = 0;
        int idef_index = UnsafeNativeMethods.CRhinoInstanceObject_InstanceDefinition(ptr, ref docId);
        if (idef_index < 0)
          return null;
        RhinoDoc doc = RhinoDoc.FromId(docId);
        return new InstanceDefinition(idef_index, doc);
      }
    }

    /// <summary>Determine if this reference uses an instance definition</summary>
    /// <param name="definitionIndex"></param>
    /// <param name="nestingLevel">
    /// If the instance definition is used, this is the definition's nesting depth
    /// </param>
    /// <returns>true or false depending on if the deifinition is used</returns>
    public bool UsesDefinition(int definitionIndex, out int nestingLevel)
    {
      nestingLevel = 0;
      IntPtr pConstThis = ConstPointer();
      int rc = UnsafeNativeMethods.CRhinoInstanceObject_UsesDefinition(pConstThis, definitionIndex);
      if (rc >= 0)
        nestingLevel = rc;
      return rc >= 0;
    }

    /// <summary>
    /// Explodes the instance reference into pieces.
    /// </summary>
    /// <param name="explodeNestedInstances">
    /// If true, then nested instance references are recursively exploded into pieces
    /// until actual geometry is found. If false, an InstanceObject is added to
    /// the pieces out parameter when this InstanceObject has nested references.
    /// </param>
    /// <param name="pieces">An array of Rhino objects will be assigned to this out parameter during this call.</param>
    /// <param name="pieceAttributes">An array of object attributes will be assigned to this out parameter during this call.</param>
    /// <param name="pieceTransforms">An array of the previously applied transform matrices will be assigned to this out parameter during this call.</param>
    public void Explode(bool explodeNestedInstances, out RhinoObject[] pieces, out ObjectAttributes[] pieceAttributes, out Transform[] pieceTransforms)
    {
      IntPtr pConstThis = ConstPointer();
      IntPtr pPieceList = UnsafeNativeMethods.CRhinoInstanceObject_Explode(pConstThis, explodeNestedInstances);
      int count = UnsafeNativeMethods.CRhinoInstanceObjectPieceArray_Count(pPieceList);
      pieces = new RhinoObject[count];
      pieceAttributes = new ObjectAttributes[count];
      pieceTransforms = new Transform[count];
      for (int i = 0; i < count; i++)
      {
        Transform xform = new Transform();
        ObjectAttributes attrs = new ObjectAttributes();
        IntPtr pAttrs = attrs.NonConstPointer();
        IntPtr pRhinoObject = UnsafeNativeMethods.CRhinoInstanceObjectPieceArray_Item(pPieceList, i, pAttrs, ref xform);
        pieces[i] = RhinoObject.CreateRhinoObjectHelper(pRhinoObject);
        pieceAttributes[i] = attrs;
        pieceTransforms[i] = xform;
      }
      UnsafeNativeMethods.CRhinoInstanceObjectPieceArray_Delete(pPieceList);
    }
  }

  public sealed class InstanceDefinition // don't derive from ON_InstanceDefinition. We want this class to be read only
  {
    private readonly int m_index;
    private readonly RhinoDoc m_doc;

    internal InstanceDefinition(int index, RhinoDoc doc)
    {
      m_index = index;
      m_doc = doc;
    }

    /// <summary>
    /// Number of objects this definition uses. This counts the objects that are used to define the geometry.
    /// This does NOT count the number of references to this instance definition.
    /// </summary>
    public int ObjectCount
    {
      get
      {
        return UnsafeNativeMethods.CRhinoInstanceDefinition_ObjectCount(m_doc.m_docId, m_index);
      }
    }

    public InstanceDefinitionUpdateType UpdateType
    {
      get
      {
        int rc = UnsafeNativeMethods.CRhinoInstanceDefinition_UpdateType(m_doc.m_docId, m_index);
        return (InstanceDefinitionUpdateType)rc;
      }
    }

    /// <summary>
    /// returns an object used as part of this definition.
    /// </summary>
    /// <param name="index">0 &lt;= index &lt; ObjectCount.</param>
    /// <returns>
    /// Returns an object that is used to define the geometry.
    /// Does NOT return an object that references this definition.count the number of references to this instance.
    /// </returns>
    public DocObjects.RhinoObject Object(int index)
    {
      IntPtr ptr = UnsafeNativeMethods.CRhinoInstanceDefinition_Object(m_doc.m_docId, m_index, index);
      return DocObjects.RhinoObject.CreateRhinoObjectHelper(ptr);
    }

    /// <summary>
    /// Gets an array with the objects that belong to this instance definition.
    /// </summary>
    /// <returns>An array of Rhino objects. The returned array can be empty, but not null.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_instancedefinitionobjects.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_instancedefinitionobjects.cs' lang='cs'/>
    /// <code source='examples\py\ex_instancedefinitionobjects.py' lang='py'/>
    /// </example>
    public DocObjects.RhinoObject[] GetObjects()
    {
      int count = ObjectCount;
      DocObjects.RhinoObject[] rc = new RhinoObject[count];
      for (int i = 0; i < count; i++)
      {
        IntPtr ptr = UnsafeNativeMethods.CRhinoInstanceDefinition_Object(m_doc.m_docId, m_index, i);
        rc[i] = DocObjects.RhinoObject.CreateRhinoObjectHelper(ptr);
      }
      return rc;
    }

    /// <summary>
    /// Gets a list of the CRhinoInstanceObjects (inserts) that contains
    /// a reference this instance definition.
    /// </summary>
    /// <param name="wheretoLook">
    /// <para>0 = get top level references in active document.</para>
    /// <para>1 = get top level and nested references in active document.</para>
    /// <para>2 = check for references from other instance definitions.</para>
    /// </param>
    /// <returns>An array of instance objects. The returned array can be empty, but not null.</returns>
    public InstanceObject[] GetReferences(int wheretoLook)
    {
      int refCount = UnsafeNativeMethods.CRhinoInstanceDefintition_GetReferences1(m_doc.m_docId, m_index, wheretoLook);
      if (refCount < 1)
        return new InstanceObject[0];
      InstanceObject[] rc = new InstanceObject[refCount];
      for (int i = 0; i < refCount; i++)
      {
        IntPtr ptr = UnsafeNativeMethods.CRhinoInstanceDefinition_GetReferences2(i);
        if (ptr != IntPtr.Zero)
        {
          uint sn = UnsafeNativeMethods.CRhinoObject_RuntimeSN(ptr);
          rc[i] = new InstanceObject(sn);
        }
      }
      UnsafeNativeMethods.CRhinoInstanceDefinition_GetReferences3();
      return rc;
    }

    /// <summary>
    /// Gets a list of all the InstanceDefinitions that contain a reference this InstanceDefinition.
    /// </summary>
    /// <returns>An array of instance definitions. The returned array can be empty, but not null.</returns>
    public InstanceDefinition[] GetContainers()
    {
      using (Runtime.InteropWrappers.SimpleArrayInt arr = new Rhino.Runtime.InteropWrappers.SimpleArrayInt())
      {
        IntPtr ptr = arr.m_ptr;
        int count = UnsafeNativeMethods.CRhinoInstanceDefinition_GetContainers(m_doc.m_docId, m_index, ptr);
        InstanceDefinition[] rc = null;
        if (count > 0)
        {
          int[] indices = arr.ToArray();
          if (indices != null)
          {
            count = indices.Length;
            rc = new InstanceDefinition[count];
            for (int i = 0; i < count; i++)
            {
              rc[i] = new InstanceDefinition(indices[i], m_doc);
            }
          }
        }
        else
          rc = new InstanceDefinition[0];
        return rc;
      }
    }

    /// <summary>
    /// Determines if this instance definition contains a reference to another instance definition.
    /// </summary>
    /// <param name="otherIdefIndex">index of another instance definition.</param>
    /// <returns>
    ///   0      no
    ///   1      other_idef_index is the index of this instance definition
    ///  >1      This InstanceDefinition uses the instance definition
    ///          and the returned value is the nesting depth.
    /// </returns>
    public int UsesDefinition(int otherIdefIndex)
    {
      return UnsafeNativeMethods.CRhinoInstanceDefinition_UsesDefinition(m_doc.m_docId, m_index, otherIdefIndex);
    }

    /// <summary>
    /// Determines whether the instance definition is referenced.
    /// </summary>
    /// <param name="wheretoLook">
    /// <para>0 = check for top level references in active document.</para>
    /// <para>1 = check for top level and nested references in active document.</para>
    /// <para>2 = check for references in other instance definitions.</para>
    /// </param>
    /// <returns>true if the instance definition is used; otherwise false.</returns>
    public bool InUse(int wheretoLook)
    {
      return UnsafeNativeMethods.CRhinoInstanceDefinition_InUse(m_doc.m_docId, m_index, wheretoLook);
    }

    /// <summary>
    /// Index of this instance definition in the index definition table.
    /// </summary>
    public int Index
    {
      get { return m_index; }
    }

    /// <summary>
    /// An object from a work session reference model is reference a
    /// reference object and cannot be modified.  An object is a reference
    /// object if, and only if, it is on a reference layer.
    /// </summary>
    public bool IsReference
    {
      get
      {
        return UnsafeNativeMethods.CRhinoInstanceDefinition_IsReference(m_doc.m_docId, m_index);
      }
    }

    public bool IsTenuous
    {
      get
      {
        return UnsafeNativeMethods.CRhinoInstanceDefinition_IsTenuous(m_doc.m_docId, m_index);
      }
    }
    /// <summary>
    /// Controls how much geometry is read when a linked InstanceDefinition is updated.
    /// </summary>
    /// <returns>If this returns true then nested linked InstanceDefinition objects will be skipped otherwise; read everything, included nested linked InstanceDefinition objects</returns>
    public bool SkipNestedLinkedDefinitions
    {
      get
      {
        return (UnsafeNativeMethods.CRhinoInstanceDefinition_UpdateDepth(m_doc.m_docId, m_index) != 0);
      }
    }

    public InstanceDefinitionLayerStyle LayerStyle
    {
      get
      {
        int layerStyle = UnsafeNativeMethods.CRhinoInstanceDefinition_LayerStyle(m_doc.m_docId, m_index);
        if (layerStyle == (int)InstanceDefinitionLayerStyle.Active)
          return InstanceDefinitionLayerStyle.Active;
        if (layerStyle == (int)InstanceDefinitionLayerStyle.Reference)
          return InstanceDefinitionLayerStyle.Reference;
        return InstanceDefinitionLayerStyle.None;
      }
    }

    public bool IsDeleted
    {
      get
      {
        return UnsafeNativeMethods.CRhinoInstanceDefinition_IsDeleted(m_doc.m_docId, m_index);
      }
    }
    //[skipping]
    //bool IsTenuous() const;
    //BOOL CRhinoInstanceDefinition::GetBBox(
    //bool UsesLayer( int layer_index ) const;
    //bool UsesLinetype( int linetype_index) const;
    //bool CRhinoInstanceDefinition::RemoveLinetypeReference( int linetype_index);

    ////////////////////////////////////////////////////////
    //from ON_InstanceDefinition
    const int idxName = 0;
    const int idxDescription = 1;
    const int idxSourceArchive = 2;
    const int idxUrlTag = 3;
    const int idxUrl = 4;

    string GetString(int which)
    {
      IntPtr ptr = UnsafeNativeMethods.CRhinoInstanceDefinition_GetString(m_doc.m_docId, m_index, which);
      if (IntPtr.Zero == ptr)
        return null;
      return Marshal.PtrToStringUni(ptr);
    }

    public string Name
    {
      get{ return GetString(idxName); }
    }

    public string Description
    {
      get{ return GetString(idxDescription); }
    }

    public Guid Id
    {
      get { return UnsafeNativeMethods.CRhinoInstanceDefinition_GetUuid(m_doc.m_docId, m_index); }
    }

    public string SourceArchive
    {
      get { return GetString(idxSourceArchive); }
    }
    /// <summary>
    /// The URL description displayed as a hyperlink in the Insert and Block UI
    /// </summary>
    public string UrlDescription
    {
      get { return GetString(idxUrlTag); }
    }
    /// <summary>
    /// The hyperlink URL that is executed when the UrlDescription hyperlink is clicked on in the Insert and Block UI
    /// </summary>
    public string Url
    {
      get { return GetString(idxUrl); }
    }

    public System.Drawing.Bitmap CreatePreviewBitmap(Rhino.Display.DefinedViewportProjection definedViewportProjection, Rhino.DocObjects.DisplayMode displayMode, System.Drawing.Size bitmapSize)
    {
      IntPtr pRhinoDib = UnsafeNativeMethods.CRhinoInstanceDefinition_GetPreviewBitmap(m_doc.m_docId, m_index, (int)definedViewportProjection, (int)displayMode, bitmapSize.Width, bitmapSize.Height);
      if (IntPtr.Zero == pRhinoDib)
        return null;

      IntPtr hBmp = UnsafeNativeMethods.CRhinoDib_Bitmap(pRhinoDib);
      System.Drawing.Bitmap rc = null;
      if (IntPtr.Zero != hBmp)
      {
        rc = System.Drawing.Image.FromHbitmap(hBmp);
      }
      UnsafeNativeMethods.CRhinoDib_Delete(pRhinoDib);
      return rc;
    }

    public System.Drawing.Bitmap CreatePreviewBitmap(Rhino.Display.DefinedViewportProjection definedViewportProjection, System.Drawing.Size bitmapSize)
    {
      return CreatePreviewBitmap(definedViewportProjection, Rhino.DocObjects.DisplayMode.Wireframe, bitmapSize);
    }

    /// <summary>
    /// Returns the archive file status of a linked instance definition.
    /// </summary>
    public InstanceDefinitionArchiveFileStatus ArchiveFileStatus
    {
      get 
      {
        int rc = UnsafeNativeMethods.RHC_RhinoInstanceArchiveFileStatus(m_doc.m_docId, m_index);
        return (InstanceDefinitionArchiveFileStatus)rc;
      }
    }
  }
}


namespace Rhino.DocObjects.Tables
{
  public sealed class InstanceDefinitionTable : IEnumerable<InstanceDefinition>, Rhino.Collections.IRhinoTable<InstanceDefinition>
  {
    private readonly RhinoDoc m_doc;
    internal InstanceDefinitionTable(RhinoDoc doc)
    {
      m_doc = doc;
    }

    /// <summary>Document that owns this table.</summary>
    public RhinoDoc Document
    {
      get { return m_doc; }
    }

    /// <summary>Number of items in the instance definitions table.</summary>
    public int Count
    {
      get
      {
        return UnsafeNativeMethods.CRhinoInstanceDefinitionTable_InstanceDefinitionCount(m_doc.m_docId, true);
      }
    }

    /// <summary>
    /// Number of items in the instance definitions table, excluding deleted definitions.
    /// </summary>
    public int ActiveCount
    {
      get
      {
        return UnsafeNativeMethods.CRhinoInstanceDefinitionTable_InstanceDefinitionCount(m_doc.m_docId, false);
      }
    }

    /// <summary>
    /// Conceptually, the InstanceDefinition table is an array of Instance
    /// definitions. The operator[] can be used to get individual instance
    /// definition. An instance definition is either active or deleted and this
    /// state is reported by IsDeleted or will be null if it has been purged
    /// from the document.
    /// </summary>
    /// <param name="index">zero based array index.</param>
    /// <returns>The instance definition at the specified index.</returns>
    public DocObjects.InstanceDefinition this[int index]
    {
      get
      {
        if (index < 0 || index >= Count)
          throw new IndexOutOfRangeException();
        // If the documents instance definition table contains a null
        // definition (the definition was purged) then return null.
        IntPtr ptr = UnsafeNativeMethods.CRhinoInstanceDefinition_GetInstanceDef(m_doc.m_docId, index);
        if (IntPtr.Zero.Equals(ptr))
          return null;
        return new InstanceDefinition(index, m_doc);
      }
    }
    /// <summary>Finds the instance definition with a given name.</summary>
    /// <param name="instanceDefinitionName">name of instance definition to search for (ignores case)</param>
    /// <param name="ignoreDeletedInstanceDefinitions">true means don't search deleted instance definitions.</param>
    /// <returns>The specified instance definition, or null if nothing matching was found.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_createblock.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_createblock.cs' lang='cs'/>
    /// <code source='examples\py\ex_createblock.py' lang='py'/>
    /// </example>
    public DocObjects.InstanceDefinition Find(string instanceDefinitionName, bool ignoreDeletedInstanceDefinitions)
    {
      int index = UnsafeNativeMethods.CRhinoInstanceDefinitionTable_FindInstanceDefinition(m_doc.m_docId,
                                                                                      instanceDefinitionName,
                                                                                      ignoreDeletedInstanceDefinitions);
      if (index < 0)
        return null;
      return new Rhino.DocObjects.InstanceDefinition(index, m_doc);
    }

    /// <summary>Finds the instance definition with a given id.</summary>
    /// <param name="instanceId">Unique id of the instance definition to search for.</param>
    /// <param name="ignoreDeletedInstanceDefinitions">true means don't search deleted instance definitions.</param>
    /// <returns>The specified instance definition, or null if nothing matching was found.</returns>
    public DocObjects.InstanceDefinition Find(Guid instanceId, bool ignoreDeletedInstanceDefinitions)
    {
      int index = UnsafeNativeMethods.CRhinoInstanceDefinitionTable_FindInstanceDefinition2(m_doc.m_docId,
                                                                                       instanceId,
                                                                                       ignoreDeletedInstanceDefinitions);
      if (index < 0)
        return null;
      return new Rhino.DocObjects.InstanceDefinition(index, m_doc);
    }

    /// <summary>
    /// Adds an instance definition to the instance definition table.
    /// </summary>
    /// <param name="name">The definition name.</param>
    /// <param name="description">The definition description.</param>
    /// <param name="basePoint">A base point.</param>
    /// <param name="geometry">An array, a list or any enumerable set of geometry.</param>
    /// <param name="attributes">An array, a list or any enumerable set of attributes.</param>
    /// <returns>
    /// &gt;=0  index of instance definition in the instance definition table. -1 on failure.
    /// </returns>
    /// <example>
    /// <code source='examples\vbnet\ex_createblock.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_createblock.cs' lang='cs'/>
    /// <code source='examples\py\ex_createblock.py' lang='py'/>
    /// </example>
    public int Add(string name, string description, Point3d basePoint, IEnumerable<GeometryBase> geometry, IEnumerable<DocObjects.ObjectAttributes> attributes)
    {
      using (Rhino.Runtime.InteropWrappers.SimpleArrayGeometryPointer g = new Runtime.InteropWrappers.SimpleArrayGeometryPointer(geometry))
      {
        IntPtr pAttributes = UnsafeNativeMethods.ON_SimpleArray_3dmObjectAttributes_New();
        if (attributes != null)
        {
          foreach (ObjectAttributes att in attributes)
          {
            IntPtr pAtt = att.ConstPointer();
            UnsafeNativeMethods.ON_SimpleArray_3dmObjectAttributes_Add(pAttributes, pAtt);
          }
        }
        IntPtr pGeometry = g.ConstPointer();
        int rc = UnsafeNativeMethods.CRhinoInstanceDefinitionTable_Add(m_doc.m_docId, name, description, basePoint, pGeometry, pAttributes);

        UnsafeNativeMethods.ON_SimpleArray_3dmObjectAttributes_Delete(pAttributes);
        return rc;
      }
    }

    /// <summary>
    /// Adds an instance definition to the instance definition table.
    /// </summary>
    /// <param name="name">The definition name.</param>
    /// <param name="description">The definition description.</param>
    /// <param name="basePoint">A base point.</param>
    /// <param name="geometry">An array, a list or any enumerable set of geometry.</param>
    /// <returns>
    /// &gt;=0  index of instance definition in the instance definition table. -1 on failure.
    /// </returns>
    /// <example>
    /// <code source='examples\cs\ex_nestedblock.cs' lang='cs'/>
    /// </example>
    public int Add(string name, string description, Point3d basePoint, IEnumerable<GeometryBase> geometry)
    {
      return Add(name, description, basePoint, geometry, null);
    }

    /// <summary>
    /// Adds an instance definition to the instance definition table.
    /// </summary>
    /// <param name="name">The definition name.</param>
    /// <param name="description">The definition description.</param>
    /// <param name="basePoint">A base point.</param>
    /// <param name="geometry">An element.</param>
    /// <param name="attributes">An attribute.</param>
    /// <returns>
    /// &gt;=0  index of instance definition in the instance definition table. -1 on failure.
    /// </returns>
    public int Add(string name, string description, Point3d basePoint, GeometryBase geometry, DocObjects.ObjectAttributes attributes)
    {
      return Add(name, description, basePoint, new GeometryBase[] { geometry }, new ObjectAttributes[] { attributes });
    }

    /// <summary>
    /// Modifies the instance definition name and description.
    /// Does not change instance definition ID or geometry.
    /// </summary>
    /// <param name="idef">The instance definition to be modified.</param>
    /// <param name="newName">The new name.</param>
    /// <param name="newDescription">The new description string.</param>
    /// <param name="quiet">
    /// If true, information message boxes pop up when illegal changes are attempted.
    /// </param>
    /// <returns>
    /// true if successful.
    /// </returns>
    public bool Modify(DocObjects.InstanceDefinition idef, string newName, string newDescription, bool quiet)
    {
      return Modify(idef.Index, newName, newDescription, quiet);
    }

    /// <summary>
    /// Modifies the instance definition name and description.
    /// Does not change instance definition ID or geometry.
    /// </summary>
    /// <param name="idefIndex">The index of the instance definition to be modified.</param>
    /// <param name="newName">The new name.</param>
    /// <param name="newDescription">The new description string.</param>
    /// <param name="quiet">
    /// If true, information message boxes pop up when illegal changes are attempted.
    /// </param>
    /// <returns>
    /// true if successful.
    /// </returns>
    public bool Modify(int idefIndex, string newName, string newDescription, bool quiet)
    {
      return UnsafeNativeMethods.CRhinoInstanceDefinitionTable_ModifyInstanceDefinition(m_doc.m_docId, idefIndex, newName, newDescription, quiet);
    }

    /// <summary>
    /// Restores the instance definition to its previous state,
    /// if the instance definition has been modified and the modification can be undone.
    /// </summary>
    /// <param name="idefIndex">The index of the instance definition to be restored.</param>
    /// <returns>true if operation succeeded.</returns>
    public bool UndoModify(int idefIndex)
    {
      return UnsafeNativeMethods.CRhinoInstanceDefinitionTable_UndoModify(m_doc.m_docId, idefIndex);
    }

    /// <summary>
    /// Modifies the instance definition geometry and replaces all references
    /// to the current definition with references to the new definition.
    /// </summary>
    /// <param name="idefIndex">The index of the instance definition to be modified.</param>
    /// <param name="newGeometry">The new geometry.</param>
    /// <param name="newAttributes">The new attributes.</param>
    /// <returns>true if operation succeeded.</returns>
    public bool ModifyGeometry(int idefIndex, IEnumerable<GeometryBase> newGeometry, IEnumerable<ObjectAttributes> newAttributes)
    {
      using (Rhino.Runtime.InteropWrappers.SimpleArrayGeometryPointer g = new Runtime.InteropWrappers.SimpleArrayGeometryPointer(newGeometry))
      {
        IntPtr pAttributes = UnsafeNativeMethods.ON_SimpleArray_3dmObjectAttributes_New();
        if (newAttributes != null)
        {
          foreach (ObjectAttributes att in newAttributes)
          {
            IntPtr pAtt = att.ConstPointer();
            UnsafeNativeMethods.ON_SimpleArray_3dmObjectAttributes_Add(pAttributes, pAtt);
          }
        }
        IntPtr pGeometry = g.ConstPointer();
        bool rc = UnsafeNativeMethods.CRhinoInstanceDefinitionTable_ModifyGeometry(m_doc.m_docId, idefIndex, pGeometry, pAttributes);

        UnsafeNativeMethods.ON_SimpleArray_3dmObjectAttributes_Delete(pAttributes);
        return rc;
      }
    }

    public bool ModifyGeometry(int idefIndex, IEnumerable<GeometryBase> newGeometry)
    {
      return ModifyGeometry(idefIndex, newGeometry, null);
    }

    public bool ModifyGeometry(int idefIndex, GeometryBase newGeometry, ObjectAttributes newAttributes)
    {
      return ModifyGeometry(idefIndex, new GeometryBase[] { newGeometry }, new ObjectAttributes[] { newAttributes });
    }

    /// <summary>
    /// Marks the source path for a linked instance definition as relative or absolute.
    /// </summary>
    /// <param name="idef">The instance definition to be marked.</param>
    /// <param name="relative">
    /// <para>If true, the path should be considered as relative.</para>
    /// <para>If false, the path should be considered as absolute.</para>
    /// </param>
    /// <param name="quiet">If true, then message boxes about erroneous parameters will not be shown.</param>
    /// <returns>
    /// true if the instance defintion could be modified.
    /// </returns>
    public bool MakeSourcePathRelative(DocObjects.InstanceDefinition idef, bool relative, bool quiet)
    {
      if (null == idef)
        return false;
      return UnsafeNativeMethods.CRhinoInstanceDefinitionTable_MakeSourcePathRelative(m_doc.m_docId, idef.Index, relative, quiet);
    }

    /// <summary>
    /// Deletes the instance definition.
    /// </summary>
    /// <param name="idefIndex">
    /// zero based index of instance definition to delete.
    /// This must be in the range 0 &lt;= idefIndex &lt; InstanceDefinitionTable.Count.
    /// </param>
    /// <param name="deleteReferences">
    /// true to delete all references to this definition.
    /// false to delete definition only if there are no references.
    /// </param>
    /// <param name="quiet">
    /// If true, no warning message box appears if an instance definition cannot be
    /// deleted because it is the current layer or it contains active geometry.
    /// </param>
    /// <returns>
    /// true if successful. false if the instance definition has active references and bDeleteReferences is false.
    /// </returns>
    public bool Delete(int idefIndex, bool deleteReferences, bool quiet)
    {
      return UnsafeNativeMethods.CRhinoInstanceDefinitionTable_DeleteInstanceDefinition(m_doc.m_docId, idefIndex, deleteReferences, quiet);
    }

  //Description:
  //  Purges an instance definition and its definition geometry.
  //Parameters:
  //  idef_index - [in] zero based index of instance definition to delete.
  //      This must be in the range 
  //      0 <= idefIndex < InstanceDefinitionTable.Count
  //Returns:
  //  True if successful. False if the instance definition cannot be purged
  //  because it is in use by reference objects or undo information.
  //bool PurgeInstanceDefinition( int idef_index );

  //Description:
  //  Purge deleted instance definition information that is not
  //  in use.  This function is time consuming and should be used
  //  in a thoughtful manner.    
  //Parameters:
  //  bIgnoreUndoReferences:
  //    If false, then deleted instance definition information
  //    that could possibly be undeleted by the Undo command
  //    will not be deleted.
  //    If true, then all deleted instance definition information
  //    is deleted.
  //void Compact( bool bIgnoreUndoReferences );

  //Description:
  //  Undeletes an instance definition that has been deleted by DeleteLayer().
  //Parameters:
  //  idef_index - [in] zero based index of an instance definition
  //      to undelete. This must be in the range
  //      0 <= idefIndex < InstanceDefinitionTable.Count
  //Returns:
  //  TRUE if successful.
  //bool UndeleteInstanceDefinition( int idef_index );

  //Description:
  //  Read the objects from a file and use them as the instance's
  //  definition geometry.
  //Parameters:
  //  idef_index - [in]
  //    instance definition index
  //  filename - [in]
  //    name of file (can be any type of file that Rhino or a plug-in can read).
  //  bUpdateNestedLinks - [in]
  //    If true and the instance definition referes to a linked instance definition,
  //    that needs to be updated, then the nested defition is also updated.
  //    If false, nested updates are skipped.
  //Returns:
  //  True if successful.
  //bool UpdateLinkedInstanceDefinition(
  //        int idef_index,
  //        const wchar_t* filename,
  //        bool bUpdateNestedLinks
  //        );

  //Description:
  //  Gets an array of pointers to layers that is sorted by
  //  the values of CRhinoInstanceDefinition::m_sort_index.
  //Parameters:
  //  sorted_list - [out] this array is filled in with
  //      CRhinoInstanceDefinition pointers sorted by
  //      the values of CRhinoInstanceDefinition::m_sort_index.
  //  bIgnoreDeleted - [in] if TRUE then deleted layers are filtered out.
  //Remarks:
  //  Use Sort() to set the values of m_sort_index.
  //void GetSortedList(
  //  ON_SimpleArray<const CRhinoInstanceDefinition*>& sorted_list,
  //  bool bIgnoreDeleted = false
  //  ) const;
    /// <summary>
    /// Gets an array of instance definitions.
    /// </summary>
    /// <param name="ignoreDeleted">If true then deleted idefs are filtered out.</param>
    /// <returns>An array of instance definitions. This can be empty, but not null.</returns>
    public DocObjects.InstanceDefinition[] GetList(bool ignoreDeleted)
    {
      Runtime.InteropWrappers.SimpleArrayInt arr = new Runtime.InteropWrappers.SimpleArrayInt();
      IntPtr ptr = arr.m_ptr;
      int count = UnsafeNativeMethods.CRhinoInstanceDefinitionTable_GetList(m_doc.m_docId, ptr, ignoreDeleted);
      DocObjects.InstanceDefinition[] rc = new InstanceDefinition[0];
      if( count>0 )
      {
        int[] indices = arr.ToArray();
        if (indices!=null && indices.Length > 0)
        {
          count = indices.Length;
          rc = new Rhino.DocObjects.InstanceDefinition[count];
          int docId = m_doc.m_docId;
          for (int i = 0; i < count; i++)
          {
            // Purged instance definitions will still be in the document as null
            // pointers so check to see if the index is pointing to a null
            // definition and if it is then put a null entry in the array.
            IntPtr idef = UnsafeNativeMethods.CRhinoInstanceDefinition_GetInstanceDef(docId, indices[i]);
            rc[i] = IntPtr.Zero.Equals(idef) ? null : new Rhino.DocObjects.InstanceDefinition(indices[i], m_doc);
          }
        }
      }
      arr.Dispose();
      return rc;
    }

    /// <summary>
    /// Gets unsed instance definition name used as default when creating
    /// new instance definitions.
    /// </summary>
    /// <returns>An unused instance definition name string.</returns>
    public string GetUnusedInstanceDefinitionName()
    {
      using (Runtime.StringHolder sh = new Rhino.Runtime.StringHolder())
      {
        IntPtr pString = sh.NonConstPointer();
        UnsafeNativeMethods.CRhinoInstanceDefinitionTable_GetUnusedName(m_doc.m_docId, pString);
        return sh.ToString();
      }
    }

    /// <summary>
    /// Gets unsed instance definition name used as default when creating
    /// new instance definitions.
    /// </summary>
    /// <param name="root">
    /// The returned name is 'root nn'  If root is empty, then 'Block' (localized) is used.
    /// </param>
    /// <returns>An unused instance definition name string.</returns>
    public string GetUnusedInstanceDefinitionName(string root)
    {
      using (Runtime.StringHolder sh = new Rhino.Runtime.StringHolder())
      {
        IntPtr pString = sh.NonConstPointer();
        UnsafeNativeMethods.CRhinoInstanceDefinitionTable_GetUnusedName2(m_doc.m_docId, root, pString);
        return sh.ToString();
      }
    }

    /// <summary>
    /// Gets unsed instance definition name used as default when creating
    /// new instance definitions.
    /// </summary>
    /// <param name="root">
    /// The returned name is 'root nn'  If root is empty, then 'Block' (localized) is used.
    /// </param>
    /// <param name="defaultSuffix">
    /// Unique names are created by appending a decimal number to the
    /// localized term for "Block" as in "Block 01", "Block 02",
    /// and so on.  When defaultSuffix is supplied, the search for an unused
    /// name begins at "Block suffix".
    /// </param>
    /// <returns>An unused instance definition name string.</returns>
    [CLSCompliant(false)]
    public string GetUnusedInstanceDefinitionName(string root, uint defaultSuffix)
    {
      using (Runtime.StringHolder sh = new Rhino.Runtime.StringHolder())
      {
        IntPtr pString = sh.NonConstPointer();
        UnsafeNativeMethods.CRhinoInstanceDefinitionTable_GetUnusedName3(m_doc.m_docId, root, defaultSuffix, pString);
        return sh.ToString();
      }
    }


    #region enumerator
    // for IEnumerable<Layer>
    public IEnumerator<InstanceDefinition> GetEnumerator()
    {
      return new Rhino.Collections.TableEnumerator<InstanceDefinitionTable, InstanceDefinition>(this);
    }

    // for IEnumerable
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return new Rhino.Collections.TableEnumerator<InstanceDefinitionTable, InstanceDefinition>(this);
    }
    #endregion
  }
#endif
}
