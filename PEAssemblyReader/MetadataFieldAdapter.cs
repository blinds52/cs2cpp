﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MetadataFieldAdapter.cs" company="">
//   
// </copyright>
// <summary>
//   
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace PEAssemblyReader
{
    using System;
    using System.Diagnostics;
    using System.Reflection.Metadata;
    using System.Reflection.PortableExecutable;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Symbols;
    using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;

    /// <summary>
    /// </summary>
    [DebuggerDisplay("Name = {Name}, Type = {FieldType.FullName}")]
    public class MetadataFieldAdapter : IField
    {
        /// <summary>
        /// </summary>
        private readonly FieldSymbol fieldDef;

        /// <summary>
        /// </summary>
        /// <param name="fieldDef">
        /// </param>
        internal MetadataFieldAdapter(FieldSymbol fieldDef)
        {
            this.fieldDef = fieldDef;
        }

        /// <summary>
        /// </summary>
        /// <param name="fieldDef">
        /// </param>
        /// <param name="genericContext">
        /// </param>
        internal MetadataFieldAdapter(FieldSymbol fieldDef, IGenericContext genericContext)
            : this(fieldDef)
        {
            this.GenericContext = genericContext;
        }

        /// <summary>
        /// </summary>
        /// <exception cref="NotImplementedException">
        /// </exception>
        public string AssemblyQualifiedName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// </summary>
        public IType DeclaringType
        {
            get
            {
                return this.fieldDef.ContainingType.ResolveGeneric(this.GenericContext);
            }
        }

        /// <summary>
        /// </summary>
        public IType FieldType
        {
            get
            {
                return this.fieldDef.Type.ResolveGeneric(this.GenericContext);
            }
        }

        /// <summary>
        /// </summary>
        public string FullName
        {
            get
            {
                var metadataTypeName = MetadataTypeName.FromNamespaceAndTypeName(this.fieldDef.ContainingNamespace.Name, this.fieldDef.Name);
                return metadataTypeName.FullName;
            }
        }

        /// <summary>
        /// </summary>
        public IGenericContext GenericContext { get; set; }

        /// <summary>
        /// </summary>
        public bool IsAbstract
        {
            get
            {
                return this.fieldDef.IsAbstract;
            }
        }

        /// <summary>
        /// </summary>
        public bool IsLiteral
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// </summary>
        public bool IsOverride
        {
            get
            {
                return this.fieldDef.IsOverride;
            }
        }

        /// <summary>
        /// </summary>
        public bool IsStatic
        {
            get
            {
                return this.fieldDef.IsStatic;
            }
        }

        /// <summary>
        /// </summary>
        public bool IsVirtual
        {
            get
            {
                return this.fieldDef.IsVirtual;
            }
        }

        /// <summary>
        /// </summary>
        public string MetadataFullName
        {
            get
            {
                return this.FullName;
            }
        }

        /// <summary>
        /// </summary>
        public string MetadataName
        {
            get
            {
                return this.Name;
            }
        }

        /// <summary>
        /// </summary>
        public IModule Module
        {
            get
            {
                return new MetadataModuleAdapter(this.fieldDef.ContainingModule);
            }
        }

        /// <summary>
        /// </summary>
        public string Name
        {
            get
            {
                return this.fieldDef.Name;
            }
        }

        /// <summary>
        /// </summary>
        public string Namespace
        {
            get
            {
                return this.fieldDef.ContainingNamespace.Name;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="obj">
        /// </param>
        /// <returns>
        /// </returns>
        public int CompareTo(object obj)
        {
            var name = obj as IName;
            if (name == null)
            {
                return 1;
            }

            var val = name.Name.CompareTo(this.Name);
            if (val != 0)
            {
                return val;
            }

            val = name.Namespace.CompareTo(this.Namespace);
            if (val != 0)
            {
                return val;
            }

            return 0;
        }

        /// <summary>
        /// </summary>
        /// <returns>
        /// </returns>
        public override string ToString()
        {
            return this.fieldDef.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        public byte[] GetFieldRVAData()
        {
            PEModuleSymbol peModuleSymbol;
            PEFieldSymbol peFieldSymbol;
            this.GetPEFieldSymbol(out peModuleSymbol, out peFieldSymbol);

            if (peFieldSymbol != null)
            {
                return this.GetFieldBody(peModuleSymbol, peFieldSymbol);
            }

            return null;
        }

        private void GetPEFieldSymbol(out PEModuleSymbol peModuleSymbol, out PEFieldSymbol peMethodSymbol)
        {
            peModuleSymbol = this.fieldDef.ContainingModule as PEModuleSymbol;
            peMethodSymbol = this.fieldDef as PEFieldSymbol;
            if (peMethodSymbol == null)
            {
                peMethodSymbol = this.fieldDef.OriginalDefinition as PEFieldSymbol;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="peModuleSymbol">
        /// </param>
        /// <param name="peFieldSymbol">
        /// </param>
        /// <returns>
        /// </returns>
        private byte[] GetFieldBody(PEModuleSymbol peModuleSymbol, PEFieldSymbol peFieldSymbol)
        {
            var peModule = peModuleSymbol.Module;
            if (peFieldSymbol != null)
            {
                var field = peModule.MetadataReader.GetField(peFieldSymbol.Handle);
                return GetFieldBody(field.GetRelativeVirtualAddress(), peModule.PEReaderOpt);
            }

            return null;
        }

        public byte[] GetFieldBody(int relativeVirtualAddress, PEReader peReader)
        {
            var peHeaders = peReader.PEHeaders;

            var containingSectionIndex = peHeaders.GetContainingSectionIndex(relativeVirtualAddress);
            if (containingSectionIndex < 0)
            {
                return null;
            }

            var num = relativeVirtualAddress - peHeaders.SectionHeaders[containingSectionIndex].VirtualAddress;
            var length = peHeaders.SectionHeaders[containingSectionIndex].VirtualSize - num;

            IntPtr pointer;
            int size;
            peReader.GetEntireImage(out pointer, out size);

            var reader = new BlobReader(pointer + peHeaders.SectionHeaders[containingSectionIndex].PointerToRawData, length);
            var bytes = reader.ReadBytes(length);

            return bytes;
        }
    }
}