﻿// Copyright (c) 2020 Daniel Grunwald

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace NullabilityInference
{
    public sealed class TypeSystem
    {
        public NullabilityNode NullableNode { get; } = new SpecialNullabilityNode(NullType.Nullable);
        public NullabilityNode NonNullNode { get; } = new SpecialNullabilityNode(NullType.NonNull);
        public NullabilityNode ObliviousNode { get; } = new SpecialNullabilityNode(NullType.Oblivious);

        private readonly Compilation compilation;
        private readonly Dictionary<SyntaxTree, SyntaxToNodeMapping> syntaxMapping = new Dictionary<SyntaxTree, SyntaxToNodeMapping>();
        private readonly Dictionary<ISymbol, TypeWithNode> symbolType = new Dictionary<ISymbol, TypeWithNode>();
        private readonly List<NullabilityNode> additionalNodes = new List<NullabilityNode>();


        private readonly INamedTypeSymbol voidType;
        public TypeWithNode VoidType => new TypeWithNode(voidType, ObliviousNode);

        public TypeSystem(Compilation compilation)
        {
            this.compilation = compilation;
            this.voidType = compilation.GetSpecialType(SpecialType.System_Void);
        }

        public TypeWithNode GetSymbolType(ISymbol symbol)
        {
            if (symbol is IParameterSymbol { ContainingSymbol: IMethodSymbol { AssociatedSymbol: IPropertySymbol prop } } p) {
                // A parameter on an accessor differs from the parameter on the surrounding indexer.
                if (p.Ordinal >= prop.Parameters.Length) {
                    Debug.Assert(p.Name == "value");
                    Debug.Assert(p.Ordinal == prop.Parameters.Length);
                    // 'value' in property setter has same type as property return type
                    return GetSymbolType(prop);
                } else {
                    symbol = prop.Parameters[p.Ordinal];
                }
            }
            if (symbolType.TryGetValue(symbol, out var type)) {
                Debug.Assert(SymbolEqualityComparer.Default.Equals(symbol.ContainingModule, compilation.SourceModule),
                    "Entries in the symbolType dictionary should be from the SourceModule.");
                return type;
            }
            Debug.Assert(!SymbolEqualityComparer.Default.Equals(symbol.ContainingModule, compilation.SourceModule),
                "Symbols from the SourceModule should be found in the symbolType dictionary.");
            switch (symbol.Kind) {
                case SymbolKind.Method:
                    var method = (IMethodSymbol)symbol;
                    return FromType(method.ReturnType, method.ReturnNullableAnnotation);
                case SymbolKind.Parameter:
                    var parameter = (IParameterSymbol)symbol;
                    return FromType(parameter.Type, parameter.NullableAnnotation);
                case SymbolKind.Property:
                    var property = (IPropertySymbol)symbol;
                    return FromType(property.Type, property.NullableAnnotation);
                case SymbolKind.Field:
                    var field = (IFieldSymbol)symbol;
                    return FromType(field.Type, field.NullableAnnotation);
                case SymbolKind.Event:
                    var ev = (IEventSymbol)symbol;
                    return FromType(ev.Type, ev.NullableAnnotation);
                default:
                    throw new NotImplementedException($"External symbol: {symbol.Kind}");
            }
        }

        internal TypeWithNode FromType(ITypeSymbol? type, NullableAnnotation nullability)
        {
            return new TypeWithNode(type ?? voidType, nullability switch
            {
                NullableAnnotation.Annotated => NullableNode,
                NullableAnnotation.NotAnnotated => NonNullNode,
                _ => ObliviousNode,
            });
        }

        public IEnumerable<NullabilityNode> AllNodes {
            get {
                yield return NullableNode;
                yield return NonNullNode;
                yield return ObliviousNode;
                foreach (var mapping in syntaxMapping.Values) {
                    foreach (var node in mapping.Nodes) {
                        yield return node;
                    }
                }
                foreach (var node in additionalNodes) {
                    yield return node;
                }
            }
        }

        internal IEnumerable<NullabilityNode> ParameterNodes {
            get {
                foreach (var (sym, type) in symbolType) {
                    if (sym.Kind == SymbolKind.Parameter) {
                        yield return type.Node;
                    }
                }
            }
        }

        internal void RegisterNodes(SyntaxTree syntaxTree, SyntaxToNodeMapping mapping)
        {
            syntaxMapping.Add(syntaxTree, mapping);
        }

        internal SyntaxToNodeMapping GetMapping(SyntaxTree syntaxTree)
        {
            return syntaxMapping[syntaxTree];
        }

        /// <summary>
        /// Caches additions to the type system, actually adding them when Flush() is called.
        /// </summary>
        /// <remarks>
        /// Neither the type-system nor the builder is thread-safe.
        /// However, multiple builders can be used concurrently
        /// </remarks>
        internal class Builder
        {
            public readonly NullabilityNode NullableNode;
            public readonly NullabilityNode NonNullNode;
            public readonly NullabilityNode ObliviousNode;
            public readonly TypeWithNode VoidType;

            public Builder(TypeSystem typeSystem)
            {
                // Don't store the typeSystem is this; we may not access it outside of Flush().
                this.NullableNode = typeSystem.NullableNode;
                this.NonNullNode = typeSystem.NonNullNode;
                this.ObliviousNode = typeSystem.ObliviousNode;
                this.VoidType = typeSystem.VoidType;
            }

            public void AddSymbolType(ISymbol symbol, TypeWithNode type)
            {
                type.SetName(symbol.Name);
                AddAction(ts => ts.symbolType.Add(symbol, type));
            }

            private readonly List<Action<TypeSystem>> cachedActions = new List<Action<TypeSystem>>();

            private void AddAction(Action<TypeSystem> action)
            {
                cachedActions.Add(action);
            }

            public void Flush(TypeSystem typeSystem)
            {
                foreach (var action in cachedActions) {
                    action(typeSystem);
                }
                cachedActions.Clear();
            }
        }

        internal void RegisterNodes(IEnumerable<NullabilityNode> newNodes)
        {
            additionalNodes.AddRange(newNodes);
        }
        internal void RegisterEdges(IEnumerable<NullabilityEdge> newEdges)
        {
            foreach (var edge in newEdges) {
                edge.Source.OutgoingEdges.Add(edge);
                edge.Target.IncomingEdges.Add(edge);
            }
        }
    }
}
