using Silksong.ModMenu.Models;
using System;

namespace EdgeDetection.Menu;

/// <summary>
/// Simple slider model which maps the index directly to a byte.
/// </summary>
internal class ByteSliderModel(byte min, byte max) : SliderModel<byte>(min, max) {
	protected override bool GetIndex(byte value, out int index) {
		index = value;
		return MinimumIndex <= index && index <= MaximumIndex;
	}
	protected override byte GetValue(int index)
		=> (byte)Math.Clamp(index, byte.MinValue, byte.MaxValue);
}
