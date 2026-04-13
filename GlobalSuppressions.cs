// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
	"Method Declaration",
	"Harmony003:Harmony non-ref patch parameters modified",
	Justification = "More annoying than helpful because it warns for non-patch methods in patch classes.",
	Scope = "namespaceanddescendants",
	Target = $"~N:{nameof(EdgeDetection)}.Patches")]