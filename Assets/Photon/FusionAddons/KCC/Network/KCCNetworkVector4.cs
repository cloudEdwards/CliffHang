namespace Fusion.Addons.KCC
{
	using System;
	using UnityEngine;

	public sealed unsafe class KCCNetworkVector4<TContext> : KCCNetworkProperty<TContext> where TContext : class
	{
		// PRIVATE MEMBERS

		private readonly float _readAccuracy;
		private readonly float _writeAccuracy;

		private readonly Action<TContext, Vector4>                        _set;
		private readonly Func<TContext, Vector4>                          _get;
		private readonly Func<TContext, float, Vector4, Vector4, Vector4> _interpolate;

		// CONSTRUCTORS

		public KCCNetworkVector4(TContext context, float accuracy, Action<TContext, Vector4> set, Func<TContext, Vector4> get, Func<TContext, float, Vector4, Vector4, Vector4> interpolate) : base(context, 4)
		{
			_readAccuracy  = accuracy > 0.0f ? accuracy        : 0.0f;
			_writeAccuracy = accuracy > 0.0f ? 1.0f / accuracy : 0.0f;

			_set         = set;
			_get         = get;
			_interpolate = interpolate;
		}

		// KCCNetworkProperty INTERFACE

		public override void Read(int* ptr)
		{
			Vector4 value = default;

			if (_readAccuracy <= 0.0f)
			{
				value.x = *(float*)(ptr + 0);
				value.y = *(float*)(ptr + 1);
				value.z = *(float*)(ptr + 2);
				value.w = *(float*)(ptr + 3);
			}
			else
			{
				value.x = (*(ptr + 0)) * _readAccuracy;
				value.y = (*(ptr + 1)) * _readAccuracy;
				value.z = (*(ptr + 2)) * _readAccuracy;
				value.w = (*(ptr + 3)) * _readAccuracy;
			}

			_set(Context, value);
		}

		public override void Write(int* ptr)
		{
			Vector4 value = _get(Context);

			if (_writeAccuracy <= 0.0f)
			{
				*(float*)(ptr + 0) = value.x;
				*(float*)(ptr + 1) = value.y;
				*(float*)(ptr + 2) = value.z;
				*(float*)(ptr + 3) = value.w;
			}
			else
			{
				*(ptr + 0) = value.x < 0.0f ? (int)((value.x * _writeAccuracy) - 0.5f) : (int)((value.x * _writeAccuracy) + 0.5f);
				*(ptr + 1) = value.y < 0.0f ? (int)((value.y * _writeAccuracy) - 0.5f) : (int)((value.y * _writeAccuracy) + 0.5f);
				*(ptr + 2) = value.z < 0.0f ? (int)((value.z * _writeAccuracy) - 0.5f) : (int)((value.z * _writeAccuracy) + 0.5f);
				*(ptr + 3) = value.w < 0.0f ? (int)((value.w * _writeAccuracy) - 0.5f) : (int)((value.w * _writeAccuracy) + 0.5f);
			}
		}

		public override void Interpolate(KCCInterpolationInfo interpolationInfo)
		{
			int offset = interpolationInfo.Offset;

			Vector4 fromValue;
			Vector4 toValue;
			Vector4 value;

			if (_readAccuracy <= 0.0f)
			{
				fromValue.x = interpolationInfo.FromBuffer.ReinterpretState<float>(offset + 0);
				fromValue.y = interpolationInfo.FromBuffer.ReinterpretState<float>(offset + 1);
				fromValue.z = interpolationInfo.FromBuffer.ReinterpretState<float>(offset + 2);
				fromValue.w = interpolationInfo.FromBuffer.ReinterpretState<float>(offset + 3);

				toValue.x = interpolationInfo.ToBuffer.ReinterpretState<float>(offset + 0);
				toValue.y = interpolationInfo.ToBuffer.ReinterpretState<float>(offset + 1);
				toValue.z = interpolationInfo.ToBuffer.ReinterpretState<float>(offset + 2);
				toValue.w = interpolationInfo.ToBuffer.ReinterpretState<float>(offset + 3);
			}
			else
			{
				fromValue.x = interpolationInfo.FromBuffer.ReinterpretState<int>(offset + 0) * _readAccuracy;
				fromValue.y = interpolationInfo.FromBuffer.ReinterpretState<int>(offset + 1) * _readAccuracy;
				fromValue.z = interpolationInfo.FromBuffer.ReinterpretState<int>(offset + 2) * _readAccuracy;
				fromValue.w = interpolationInfo.FromBuffer.ReinterpretState<int>(offset + 3) * _readAccuracy;

				toValue.x = interpolationInfo.ToBuffer.ReinterpretState<int>(offset + 0) * _readAccuracy;
				toValue.y = interpolationInfo.ToBuffer.ReinterpretState<int>(offset + 1) * _readAccuracy;
				toValue.z = interpolationInfo.ToBuffer.ReinterpretState<int>(offset + 2) * _readAccuracy;
				toValue.w = interpolationInfo.ToBuffer.ReinterpretState<int>(offset + 3) * _readAccuracy;
			}

			if (_interpolate != null)
			{
				value = _interpolate(Context, interpolationInfo.Alpha, fromValue, toValue);
			}
			else
			{
				value = Vector4.Lerp(fromValue, toValue, interpolationInfo.Alpha);
			}

			_set(Context, value);
		}
	}
}
