using GlobalEnums;
using System;

namespace EdgeDetection.Structs;

/// <summary>
/// Deserialization struct which describes an <see cref="Components.EdgeDetectionPass"/>.
/// </summary>
[Serializable]
internal record struct PassDef(
	string Id,
	Color Colour,
	byte Width,
	bool HalfRes,
	PhysLayers[] Layers,
	float Threshold,
	float ClipFar,
	float ClipNear);
