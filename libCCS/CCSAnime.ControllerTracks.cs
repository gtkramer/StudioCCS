/*
 * Created by SharpDevelop.
 * User: NCDyson
 * Date: 7/25/2017
 * Time: 3:45 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Mathematics;
using System.Diagnostics;

namespace StudioCCS.libCCS
{
	public partial class CCSAnime : CCSBaseObject
	{
		public interface IAnimationKey<T>
		{
			void Read(BinaryReader bStream, int _frameNum);
			T Value();
			int FrameNumber();
			void SetFrameCount(int fCount);
			int GetFrameCount();
		}
		
		public class Vec4Key_Color : IAnimationKey<Vector4>
		{
			int FrameNum;
			int FrameCount;
			Vector4 KeyValue;
			
			#region IAnimationKey implementation
			public void Read(BinaryReader bStream, int _frameNum)
			{
				FrameNum = _frameNum;
				KeyValue = Util.ReadVec4RGBA32(bStream);
			}
			
			public Vector4 Value()
			{
				return KeyValue;
			}
			
			public int FrameNumber()
			{
				return FrameNum;
			}
			
			public int GetFrameCount()
			{
				return FrameCount;
			}
			
			public void SetFrameCount(int fCount)
			{
				FrameCount = fCount;
			}
			
			#endregion
		}
		
		public class Vec4Key_Rotation : IAnimationKey<Quaternion>
		{
			int FrameNum;
			int FrameCount;
			Quaternion KeyValue;
			
			#region IAnimationKey implemenetation
			public void Read(BinaryReader bStream, int _frameNum)
			{
				FrameNum = _frameNum;
				KeyValue = new Quaternion(bStream.ReadSingle(), bStream.ReadSingle(), bStream.ReadSingle(), bStream.ReadSingle());
			}
			
			public Quaternion Value()
			{
				return KeyValue;
			}
			
			public int FrameNumber()
			{
				return FrameNum;
			}
			
			public int GetFrameCount()
			{
				return FrameCount;
			}
			
			public void SetFrameCount(int fCount)
			{
				FrameCount = fCount;
			}
			#endregion
		}
		
		public class Vec3Key_Rotation : IAnimationKey<Vector3>
		{
			Vector3 KeyValue;
			int FrameNum;
			int FrameCount;
			#region IAnimationKey implementation
			public void Read(BinaryReader bStream, int _frameNum)
			{
				FrameNum = _frameNum;
				KeyValue = Util.ReadVec3Rotation(bStream);
			}
			
			public Vector3 Value()
			{
				return KeyValue;
			}
			
			public int FrameNumber()
			{
				return FrameNum;
			}
			
			public int GetFrameCount()
			{
				return FrameCount;
			}
			
			public void SetFrameCount(int fCount)
			{
				FrameCount = fCount;
			}
			#endregion
		}
		
		public class Vec3Key_Position : IAnimationKey<Vector3>
		{
			Vector3 KeyValue;
			int FrameNum;
			int FrameCount;
			
			#region IAnimationKey implementation
			public void Read(BinaryReader bStream, int _frameNum)
			{
				FrameNum = _frameNum;
				KeyValue = Util.ReadVec3Position(bStream);
			}
			
			public Vector3 Value()
			{
				return KeyValue;
			}
			
			public int FrameNumber()
			{
				return FrameNum;
			}
			
			public int GetFrameCount()
			{
				return FrameCount;
			}
			
			public void SetFrameCount(int fCount)
			{
				FrameCount = fCount;
			}
			#endregion
		}
		
		public class Vec3Key_Scale : IAnimationKey<Vector3>
		{
			Vector3 KeyValue = Vector3.One;
			int FrameNum;
			int FrameCount;
			#region IAnimationKey implementation
			public void Read(BinaryReader bStream, int _frameNum)
			{
				FrameNum = _frameNum;
				KeyValue = Util.ReadVec3Scale(bStream);
			}
			
			public Vector3 Value()
			{
				return KeyValue;
			}
			
			public int FrameNumber()
			{
				return FrameNum;
			}
			
			public int GetFrameCount()
			{
				return FrameCount;
			}
			
			public void SetFrameCount(int fCount)
			{
				FrameCount = fCount;
			}
			#endregion
		}
		
		public class Vec2Key_UV : IAnimationKey<Vector2>
		{
			int FrameNum;
			int FrameCount;
			Vector2 KeyValue;
			#region IAnimationKey implementation
			public void Read(BinaryReader bStream, int _frameNum)
			{
				FrameNum = _frameNum;
				KeyValue = Util.ReadVec2UV(bStream);
			}
			
			public Vector2 Value()
			{
				return KeyValue;
			}
			
			public int FrameNumber()
			{
				return FrameNum;
			}
			
			public int GetFrameCount()
			{
				return FrameCount;
			}
			
			public void SetFrameCount(int fCount)
			{
				FrameCount = fCount;
			}
			#endregion
		}
		
		public class F32Key : IAnimationKey<float>
		{
			int FrameNum;
			int FrameCount;
			float KeyValue;
			#region IAnimationKey implementation
			public void Read(BinaryReader bStream, int _frameNum)
			{
				FrameNum = _frameNum;
				KeyValue = bStream.ReadSingle();
			}
			
			public float Value()
			{
				return KeyValue;
			}
			
			public int FrameNumber()
			{
				return FrameNum;
			}
			
			public int GetFrameCount()
			{
				return FrameCount;
			}
			
			public void SetFrameCount(int fCount)
			{
				FrameCount = fCount;
			}
			#endregion
		}
		
		public class Int32Key : IAnimationKey<int>
		{
			public int FrameNum;
			public int KeyValue;
			public int FrameCount;
			#region IAnimationKey implementation
			public void Read(BinaryReader bStream, int _frameNum)
			{
				FrameNum = _frameNum;
				KeyValue = bStream.ReadInt32();
			}
			
			public int Value()
			{
				return KeyValue;
			}
			
			public int FrameNumber()
			{
				return FrameNum;
			}
			
			public int GetFrameCount()
			{
				return FrameCount;
			}
			
			public void SetFrameCount(int fCount)
			{
				FrameCount = fCount;
			}
			#endregion
		}
		
		
		//TODO: CCAnime Controller Tracks: Can these be templated without "abusing" generics?

/*		
#region Auto-Generated with meta.py

	public class Vec4Rotation_Track
	{
		public List<Vec4Key_Rotation> Keys = new List<Vec4Key_Rotation>();
		public Vec4Key_Rotation FixedValue = new Vec4Key_Rotation();
		public int KeyCount = 0;
		public int CurrentKey = 0;
		
		public Vec4Key_Rotation GetValue(int keyID)
		{
			if(KeyCount == 0)
			{
				return FixedValue;
			}
			else if(keyID < KeyCount)
			{
				return Keys[keyID];
			}
			return Keys[Keys.Count - 1];
		}
		
		public void Read(BinaryReader bStream, int TrackType)
		{
			if(TrackType == CCS_ANIME_CONTROLLER_TYPE_FIXED)
			{
				FixedValue.Read(bStream, 0);
			}
			else if(TrackType == CCS_ANIME_CONTROLLER_TYPE_ANIMATED)
			{
				KeyCount = bStream.ReadInt32();
				for(int i = 0; i < KeyCount; i++)
				{
					int frameNum = bStream.ReadInt32();
					var tmpKey = new Vec4Key_Rotation();
					tmpKey.Read(bStream, frameNum);
					if(Keys.Count > 0)
					{
						var lastKeyFrame = Keys[Keys.Count - 1];
						if(lastKeyFrame.FrameNumber() == frameNum)
						{
							//Kill duplicated keyframe
							//TODO: Vec4Unk_Track: Properly handle duplicate keyframes
							Keys.Remove(lastKeyFrame);
						}
					}
					Keys.Add(tmpKey);
					if(Keys.Count > 1)
					{
						int LastKeyID = Keys.Count - 1;
						Keys[LastKeyID].SetFrameCount(tmpKey.FrameNumber());
					}
				}
				int LastKeyID = Keys.Count - 1;
				Keys[LastKeyID].SetFrameCount(FrameC
				//TODO: Vec4Unk_Track::Read(): Warn about unkown track type.
			}
		}
		
        public Quaternion GetInterpolatedValue(int frameNumber, int frameCount)
        {
        	if(KeyCount == 0) return FixedValue.Value();
        	
        	var CurrentValue = GetValue(CurrentKey);
        	var NextValue = GetValue(CurrentKey + 1);
        	if(frameNumber >= NextValue.FrameNumber())
        	{        		
        		CurrentKey += 1;
        		if(CurrentKey > (KeyCount - 1)) CurrentKey = 0;
        		CurrentValue = GetValue(CurrentKey);
        		NextValue = GetValue(CurrentKey + 1);
        	}
        	
        	float range = 1.0f / Util.GetRangeOfFrames(CurrentValue.FrameNumber(), NextValue.FrameNumber(), frameCount);
        	int percent = frameNumber - CurrentValue.FrameNumber();
        	return Quaternion.Slerp(CurrentValue.Value(), NextValue.Value(), range *percent); //Util.Slerp(range * percent, CurrentValue.Value(), NextValue.Value());
        }
	}

    public class Vec4Color_Track
    {
        public List<Vec4Key_Color> Keys = new List<Vec4Key_Color>();
        public Vec4Key_Color FixedValue = new Vec4Key_Color();
        int KeyCount = 0;

        public Vec4Key_Color GetValue(int keyID)
        {
            if(KeyCount == 0)
            {
                return FixedValue;
            }
            else if(keyID < KeyCount)
            {
                return Keys[keyID];
            }
            return Keys[Keys.Count - 1];
        }

        public void Read(BinaryReader bStream, int TrackType)
        {
            if(TrackType == CCS_ANIME_CONTROLLER_TYPE_FIXED)
            {
                FixedValue.Read(bStream, 0);
            }
            else if(TrackType == CCS_ANIME_CONTROLLER_TYPE_ANIMATED)
            {
                KeyCount = bStream.ReadInt32();
                for(int i = 0; i < KeyCount; i++)
                {
                    int frameNum = bStream.ReadInt32();
                    //Debug.WriteLine("\t\tReading Color Track Key {0} of {1}, Frame: {2}", i, KeyCount, frameNum);
                    var tmpKey = new Vec4Key_Color();
                    tmpKey.Read(bStream, frameNum);
                    if(Keys.Count > 0)
                    {
                        Vec4Key_Color lastKeyframe = Keys[Keys.Count - 1];
                        if(lastKeyframe.FrameNumber() == frameNum)
                        {
                            //kill duplicate keyframe...
                            //TODO: Vec4Color_Track: Properly handle duplicate keyframes
                            Keys.Remove(lastKeyframe);
                        }
                    }
                    Keys.Add(tmpKey);
                }
            }
            //TODO: Vec4Color_Track::Read(): Warn about unkown track type.
        }
    }

    public class Vec3Rotation_Track
    {
        public List<Vec3Key_Rotation> Keys = new List<Vec3Key_Rotation>();
        public Vec3Key_Rotation FixedValue = new Vec3Key_Rotation();
        int KeyCount = 0;
        int CurrentKey = 0;
        
        public Vec3Key_Rotation GetValue(int keyID)
        {
            if(KeyCount == 0)
            {
                return FixedValue;
            }
            else if(keyID < KeyCount)
            {
                return Keys[keyID];
            }
            return Keys[Keys.Count - 1];
        }

        public void Read(BinaryReader bStream, int TrackType)
        {
            if(TrackType == CCS_ANIME_CONTROLLER_TYPE_FIXED)
            {
                FixedValue.Read(bStream, 0);
            }
            else if(TrackType == CCS_ANIME_CONTROLLER_TYPE_ANIMATED)
            {
                KeyCount = bStream.ReadInt32();
                for(int i = 0; i < KeyCount; i++)
                {
                    int frameNum = bStream.ReadInt32();
                    //Debug.WriteLine("\t\tReading Rotation Track Key {0} of {1}, Frame: {2}", i, KeyCount, frameNum);
                    var tmpKey = new Vec3Key_Rotation();
                    tmpKey.Read(bStream, frameNum);
                    if(Keys.Count > 0)
                    {
                        Vec3Key_Rotation lastKeyframe = Keys[Keys.Count - 1];
                        if(lastKeyframe.FrameNumber() == frameNum)
                        {
                            //kill duplicate keyframe...
                            //TODO: Vec3Rotation_Track: Properly handle duplicate keyframes
                            Keys.Remove(lastKeyframe);
                        }
                    }
                    Keys.Add(tmpKey);
                }
            }
            //TODO: Vec3Rotation_Track::Read(): Warn about unkown track type.
        }
    
        
        public Vector3 GetInterpolatedValue(int frameNumber, int frameCount)
        {
        	if(KeyCount == 0) return FixedValue.Value();
        
			        	
        	//var CurrentValue = GetValue(CurrentKey);
        	//var NextValue = GetValue(CurrentKey + 1);
        	//if(frameNumber >= NextValue.FrameNumber())
        	//{        		
        	//	CurrentKey += 1;
        	//	if(CurrentKey > (KeyCount - 1)) CurrentKey = 0;
        	//	CurrentValue = GetValue(CurrentKey);
        	//	NextValue = GetValue(CurrentKey + 1);
        	//}
        	
        	//float range = 1.0f / Util.GetRangeOfFrames(CurrentValue.FrameNumber(), NextValue.FrameNumber(), frameCount);
        	//int percent = frameNumber - CurrentValue.FrameNumber();
        	//return Util.Slerp(range * percent, CurrentValue.Value(), NextValue.Value());
        	
        	
        	//TODO: Just hold a reference to next key value?
        	
        	int NextKey = CurrentKey + 1;
        	if(NextKey > (KeyCount - 1)) NextKey = 0;
        	
        	var CurrentValue = GetValue(CurrentKey);
        	var NextValue = GetValue(NextKey);
        	
        	
        	if(frameNumber > NextValue.FrameNumber())
        	{
        		CurrentKey = NextKey;
        		NextKey = CurrentKey + 1;
        		
        	}
        	
        	
        }
    }
    

    public class Vec3Position_Track
    {
        public List<Vec3Key_Position> Keys = new List<Vec3Key_Position>();
        public Vec3Key_Position FixedValue = new Vec3Key_Position();
        int KeyCount = 0;
        int CurrentKey = 0;

        public Vec3Key_Position GetValue(int keyID)
        {
            if(KeyCount == 0)
            {
                return FixedValue;
            }
            else if(keyID < KeyCount)
            {
                return Keys[keyID];
            }
            return Keys[Keys.Count - 1];
        }

        public void Read(BinaryReader bStream, int TrackType)
        {
            if(TrackType == CCS_ANIME_CONTROLLER_TYPE_FIXED)
            {
                FixedValue.Read(bStream, 0);
            }
            else if(TrackType == CCS_ANIME_CONTROLLER_TYPE_ANIMATED)
            {
                KeyCount = bStream.ReadInt32();
                for(int i = 0; i < KeyCount; i++)
                {
                    int frameNum = bStream.ReadInt32();
                    //Debug.WriteLine("\t\tReading Position Track Key {0} of {1}, Frame: {2}", i, KeyCount, frameNum);
                    var tmpKey = new Vec3Key_Position();
                    tmpKey.Read(bStream, frameNum);
                    if(Keys.Count > 0)
                    {
                        Vec3Key_Position lastKeyframe = Keys[Keys.Count - 1];
                        if(lastKeyframe.FrameNumber() == frameNum)
                        {
                            //kill duplicate keyframe...
                            //TODO: Vec3Position_Track: Properly handle duplicate keyframes
                            Keys.Remove(lastKeyframe);
                        }
                    }
                    Keys.Add(tmpKey);
                }
            }
            
            //TODO: Vec3Position_Track::Read(): Warn about unkown track type.
        }
        
        public Vector3 GetInterpolatedValue(int frameNumber, int frameCount)
        {
        	if(KeyCount == 0) return FixedValue.Value();
        	
        	var CurrentValue = GetValue(CurrentKey);
        	var NextValue = GetValue(CurrentKey + 1);
        	if(frameNumber > NextValue.FrameNumber())
        	{
        		if(CurrentKey > (KeyCount - 1)) CurrentKey = 0;
        		CurrentValue = GetValue(CurrentKey);
        		NextValue = GetValue(CurrentKey + 1);
        	}
        	
        	float range = 1.0f / Util.GetRangeOfFrames(CurrentValue.FrameNumber(), NextValue.FrameNumber(), frameCount);
        	int percent = frameNumber - CurrentValue.FrameNumber();
        	return Vector3.Lerp(CurrentValue.Value(), NextValue.Value(), range * percent);
        }
    }

    public class Vec3Scale_Track
    {
        public List<Vec3Key_Scale> Keys = new List<Vec3Key_Scale>();
        public Vec3Key_Scale FixedValue = new Vec3Key_Scale();
        int KeyCount = 0;
        int CurrentKey = 0;

        public Vec3Key_Scale GetValue(int keyID)
        {
            if(KeyCount == 0)
            {
                return FixedValue;
            }
            else if(keyID < KeyCount)
            {
                return Keys[keyID];
            }
            return Keys[Keys.Count - 1];
        }

        public void Read(BinaryReader bStream, int TrackType)
        {
            if(TrackType == CCS_ANIME_CONTROLLER_TYPE_FIXED)
            {
                FixedValue.Read(bStream, 0);
            }
            else if(TrackType == CCS_ANIME_CONTROLLER_TYPE_ANIMATED)
            {
                KeyCount = bStream.ReadInt32();
                for(int i = 0; i < KeyCount; i++)
                {
                    int frameNum = bStream.ReadInt32();
                    //Debug.WriteLine("\t\tReading Scale Track Key {0} of {1}, Frame: {2}", i, KeyCount, frameNum);
                    var tmpKey = new Vec3Key_Scale();
                    tmpKey.Read(bStream, frameNum);
                    if(Keys.Count > 0)
                    {
                        Vec3Key_Scale lastKeyframe = Keys[Keys.Count - 1];
                        if(lastKeyframe.FrameNumber() == frameNum)
                        {
                            //kill duplicate keyframe...
                            //TODO: Vec3Scale_Track: Properly handle duplicate keyframes
                            Keys.Remove(lastKeyframe);
                        }
                    }
                    Keys.Add(tmpKey);
                }
            }
            //TODO: Vec3Scale_Track::Read(): Warn about unkown track type.
        }
        
        public Vector3 GetInterpolatedValue(int frameNumber, int frameCount)
        {
        	if(KeyCount == 0) return FixedValue.Value();
        	
        	var CurrentValue = GetValue(CurrentKey);
        	var NextValue = GetValue(CurrentKey + 1);
        	if(CurrentKey > NextValue.FrameNumber())
        	{
        		CurrentKey += 1;
        		if(CurrentKey > (KeyCount - 1)) CurrentKey = 0;
        		CurrentValue = GetValue(CurrentKey);
        		NextValue = GetValue(CurrentKey + 1);
        	}
        	
        	float range = 1.0f / Util.GetRangeOfFrames(CurrentValue.FrameNumber, NextValue.FrameNumber, frameCount);
        	int percent = frameNumber - CurrentValue.FrameNumber();
        	return Vector3.Lerp(CurrentValue.Value(), NextValue.Value(), range * percent);
        }
        
    }

    public class Vec2UV_Track
    {
        public List<Vec2Key_UV> Keys = new List<Vec2Key_UV>();
        public Vec2Key_UV FixedValue = new Vec2Key_UV();
        int KeyCount = 0;

        public Vec2Key_UV GetValue(int keyID)
        {
            if(KeyCount == 0)
            {
                return FixedValue;
            }
            else if(keyID < KeyCount)
            {
                return Keys[keyID];
            }
            return Keys[Keys.Count - 1];
        }

        public void Read(BinaryReader bStream, int TrackType)
        {
            if(TrackType == CCS_ANIME_CONTROLLER_TYPE_FIXED)
            {
                FixedValue.Read(bStream, 0);
            }
            else if(TrackType == CCS_ANIME_CONTROLLER_TYPE_ANIMATED)
            {
                KeyCount = bStream.ReadInt32();
                for(int i = 0; i < KeyCount; i++)
                {
                    int frameNum = bStream.ReadInt32();
                    //Debug.WriteLine("\t\tReading Texture Offset Track Key {0} of {1}, Frame: {2}", i, KeyCount, frameNum);
                    var tmpKey = new Vec2Key_UV();
                    tmpKey.Read(bStream, frameNum);
                    if(Keys.Count > 0)
                    {
                        Vec2Key_UV lastKeyframe = Keys[Keys.Count - 1];
                        if(lastKeyframe.FrameNumber() == frameNum)
                        {
                            //kill duplicate keyframe...
                            //TODO: Vec2UV_Track: Properly handle duplicate keyframes
                            Keys.Remove(lastKeyframe);
                        }
                    }
                    Keys.Add(tmpKey);
                }
            }
            //TODO: Vec2UV_Track::Read(): Warn about unkown track type.
        }
    }

    public class F32_Track
    {
        public List<F32Key> Keys = new List<F32Key>();
        public F32Key FixedValue = new F32Key();
        int KeyCount = 0;

        public F32Key GetValue(int keyID)
        {
            if(KeyCount == 0)
            {
                return FixedValue;
            }
            else if(keyID < KeyCount)
            {
                return Keys[keyID];
            }
            return Keys[Keys.Count - 1];
        }

        public void Read(BinaryReader bStream, int TrackType)
        {
            if(TrackType == CCS_ANIME_CONTROLLER_TYPE_FIXED)
            {
                FixedValue.Read(bStream, 0);
            }
            else if(TrackType == CCS_ANIME_CONTROLLER_TYPE_ANIMATED)
            {
                KeyCount = bStream.ReadInt32();
                for(int i = 0; i < KeyCount; i++)
                {
                    int frameNum = bStream.ReadInt32();
                    //Debug.WriteLine("\t\tReading Float Track Key {0} of {1}, Frame: {2}", i, KeyCount, frameNum);
                    var tmpKey = new F32Key();
                    tmpKey.Read(bStream, frameNum);
                    if(Keys.Count > 0)
                    {
                        F32Key lastKeyframe = Keys[Keys.Count - 1];
                        if(lastKeyframe.FrameNumber() == frameNum)
                        {
                            //kill duplicate keyframe...
                            //TODO: F32_Track: Properly handle duplicate keyframes
                            Keys.Remove(lastKeyframe);
                        }
                    }
                    Keys.Add(tmpKey);
                }
            }
            //TODO: F32_Track::Read(): Warn about unkown track type.
        }
    }

    public class Int32_Track
    {
        public List<Int32Key> Keys = new List<Int32Key>();
        public Int32Key FixedValue = new Int32Key();
        int KeyCount = 0;

        public Int32Key GetValue(int keyID)
        {
            if(KeyCount == 0)
            {
                return FixedValue;
            }
            else if(keyID < KeyCount)
            {
                return Keys[keyID];
            }
            return Keys[Keys.Count - 1];
        }

        public void Read(BinaryReader bStream, int TrackType)
        {
            if(TrackType == CCS_ANIME_CONTROLLER_TYPE_FIXED)
            {
                FixedValue.Read(bStream, 0);
            }
            else if(TrackType == CCS_ANIME_CONTROLLER_TYPE_ANIMATED)
            {
                KeyCount = bStream.ReadInt32();
                for(int i = 0; i < KeyCount; i++)
                {
                    int frameNum = bStream.ReadInt32();
                    //Debug.WriteLine("\t\tReading Int Track Key {0} of {1}, Frame: {2}", i, KeyCount, frameNum);
                    var tmpKey = new Int32Key();
                    tmpKey.Read(bStream, frameNum);
                    if(Keys.Count > 0)
                    {
                        Int32Key lastKeyframe = Keys[Keys.Count - 1];
                        if(lastKeyframe.FrameNumber() == frameNum)
                        {
                            //kill duplicate keyframe...
                            //TODO: Int32_Track: Properly handle duplicate keyframes
                            Keys.Remove(lastKeyframe);
                        }
                    }
                    Keys.Add(tmpKey);
                }
            }
            //TODO: Int32_Track::Read(): Warn about unkown track type.
        }
    }

#endregion
*/

#region Auto-Generated with meta.py
    public class Vec4Color_Track
    {
        public List<Vec4Key_Color> Keys = new List<Vec4Key_Color>();
        public Vec4Key_Color FixedValue = new Vec4Key_Color();
        int KeyCount = 0;
        int CurrentKey = 0;

        public Vec4Key_Color GetValue(int keyID)
        {
            if(KeyCount == 0)
            {
                return FixedValue;
            }
            else if(keyID < Keys.Count)
            {
                return Keys[keyID];
            }
            return Keys[Keys.Count - 1];
        }

        public void Read(BinaryReader bStream, int TrackType, int frameCount)
        {
            if(TrackType == CCS_ANIME_CONTROLLER_TYPE_FIXED)
            {
                FixedValue.Read(bStream, 0);
            }
            else if(TrackType == CCS_ANIME_CONTROLLER_TYPE_ANIMATED)
            {
                KeyCount = bStream.ReadInt32();
                for(int i = 0; i < KeyCount; i++)
                {
                    int frameNum = bStream.ReadInt32();
                    Debug.WriteLine("\t\tReading Color Track Key {0} of {1}, Frame: {2}", i, KeyCount, frameNum);
                    var tmpKey = new Vec4Key_Color();
                    tmpKey.Read(bStream, frameNum);
                    if(Keys.Count > 0)
                    {
                        Vec4Key_Color lastKeyframe = Keys[Keys.Count - 1];
                        if(lastKeyframe.FrameNumber() == frameNum)
                        {
                            //kill duplicate keyframe...
                            //TODO: Vec4Color_Track: Properly handle duplicate keyframes
                            //Keys.Remove(lastKeyframe);
                        }
                    }
                    if(Keys.Count > 0)
                    {
                        int LastKeyID = Keys.Count - 1;
                        Keys[LastKeyID].SetFrameCount(tmpKey.FrameNumber());
                    }
                    Keys.Add(tmpKey);
                }
                int FinalKeyID = Keys.Count - 1;
                Keys[FinalKeyID].SetFrameCount(frameCount);
            }
            //TODO: Vec4Color_Track::Read(): Warn about unkown track type.
        }

        public Vector4 GetInterpolatedValue(int frameNumber)
        {
            //Placeholder
            //TODO: Vec4Color_Track::GetInterpolatedValue();
            if(KeyCount == 0) return FixedValue.Value();

            if(frameNumber == 0) CurrentKey = 0;
            
            return GetValue(CurrentKey).Value();
        }
    }

    public class Vec3Rotation_Track
    {
        public List<Vec3Key_Rotation> Keys = new List<Vec3Key_Rotation>();
        public Vec3Key_Rotation FixedValue = new Vec3Key_Rotation();
        int KeyCount = 0;
        int CurrentKey = 0;

        public Vec3Key_Rotation GetValue(int keyID)
        {
            if(KeyCount == 0)
            {
                return FixedValue;
            }
            else if(keyID < Keys.Count)
            {
                return Keys[keyID];
            }
            return Keys[Keys.Count - 1];
        }

        public void Read(BinaryReader bStream, int TrackType, int frameCount)
        {
            if(TrackType == CCS_ANIME_CONTROLLER_TYPE_FIXED)
            {
                FixedValue.Read(bStream, 0);
            }
            else if(TrackType == CCS_ANIME_CONTROLLER_TYPE_ANIMATED)
            {
                KeyCount = bStream.ReadInt32();
                for(int i = 0; i < KeyCount; i++)
                {
                    int frameNum = bStream.ReadInt32();
                    Debug.WriteLine("\t\tReading Rotation Track Key {0} of {1}, Frame: {2}", i, KeyCount, frameNum);
                    var tmpKey = new Vec3Key_Rotation();
                    tmpKey.Read(bStream, frameNum);
                    if(Keys.Count > 0)
                    {
                        Vec3Key_Rotation lastKeyframe = Keys[Keys.Count - 1];
                        if(lastKeyframe.FrameNumber() == frameNum)
                        {
                            //kill duplicate keyframe...
                            //TODO: Vec3Rotation_Track: Properly handle duplicate keyframes
                            //Keys.Remove(lastKeyframe);
                        }
                    }
                    
                    if(Keys.Count > 0)
                    {
                        int LastKeyID = Keys.Count - 1;
                        Keys[LastKeyID].SetFrameCount(tmpKey.FrameNumber());
                    }
                    Keys.Add(tmpKey);
                }
                int FinalKeyID = Keys.Count - 1;
                Keys[FinalKeyID].SetFrameCount(frameCount);
            }
            //TODO: Vec3Rotation_Track::Read(): Warn about unkown track type.
        }

        public Vector3 GetInterpolatedValue(int frameNumber, string filename)
        {
            //if(KeyCount == 0) return FixedValue.Value();

            if (frameNumber == 0) CurrentKey = 0;

            var CurrentValue = GetValue(CurrentKey);
            if (frameNumber >= CurrentValue.GetFrameCount())
            {
                CurrentKey += 1;
                if (CurrentKey > (Keys.Count - 1)) CurrentKey = 0;
            }
            CurrentValue = GetValue(CurrentKey);

            int NextKey = CurrentKey + 1;
            if (NextKey > (Keys.Count - 1))
            {

                NextKey = 0;
            }
            var NextValue = GetValue(NextKey);

            Vector3 temp = new Vector3(1, 1, 1);
            temp.X = CurrentValue.Value().X;
            temp.Y = CurrentValue.Value().Y;
            temp.Z = CurrentValue.Value().Z;
            if (((Math.Abs(CurrentValue.Value().Z) > 2.85 && Math.Abs(CurrentValue.Value().Z) < 3.15) &&
            (int)Math.Abs(CurrentValue.Value().X) == 1) ||
            ((int)Math.Abs(CurrentValue.Value().Z) == 2 && (int)Math.Abs(CurrentValue.Value().X) == 1))
            {
                temp.Y = -temp.Y;
                temp.X = -temp.X;
            }
            if (((Math.Round(Math.Abs(CurrentValue.Value().Z), 1) == 1.5) || Math.Round(Math.Abs(CurrentValue.Value().Z), 1) == 1.6))
            {
                //temp.Y = -temp.Y;
                temp.X = -temp.X;
            }

            Vector3 temp2 = new Vector3(1, 1, 1);
            temp2.X = CurrentValue.Value().X;
            temp2.Y = -CurrentValue.Value().Y;
            temp2.Z = CurrentValue.Value().Z;

            Vector3 temp2next = new Vector3(1, 1, 1);
            temp2next.X = NextValue.Value().X;
            temp2next.Y = -NextValue.Value().Y;
            temp2next.Z = NextValue.Value().Z;

            if (((Math.Abs(CurrentValue.Value().Z) > 2.85 && Math.Abs(CurrentValue.Value().Z) < 3.15) &&
            (int)Math.Abs(CurrentValue.Value().X) == 1) ||
            ((int)Math.Abs(CurrentValue.Value().Z) == 2 && (int)Math.Abs(CurrentValue.Value().X) == 1))
            {
                //temp2.Y = -temp2.Y;
                //temp2.X = -temp2.X;
                //temp2.Z = -temp2.Z;
            }
            if (((Math.Round(Math.Abs(CurrentValue.Value().Z), 1) == 1.5) || Math.Round(Math.Abs(CurrentValue.Value().Z), 1) == 1.6))
            {
                //temp2.Y = -temp2.Y;
                //temp2.X = -temp2.X;
                //temp2.Z = -temp2.Z;
            }

            switch (filename)
            {
                case "OBJ_trall":
                    //temp2.X = Util.toRads(0);
                    //temp2.Y = Util.toRads(0);
                    //temp2.Z = Util.toRads(0);
                    //temp2.Z = -temp2.Z;
                    break;
                case "OBJ_t0":
                    //temp2.X = Util.toRads(180);
                    //temp2.Y = Util.toRads(90);
                    //temp2.Z = Util.toRads(0);
                    //temp2.Y -= Util.toRads(90);
                    //temp2.Y -= Util.toRads(90);
                    //temp2.X += Util.toRads(90);
                    //temp2.Z -= Util.toRads(90);
                    //temp2.Z = -temp2.Z;
                    temp2.X = -temp2.X;
                    temp2.Y = -temp2.Y;
                    temp2next.X = -temp2next.X;
                    temp2next.Y = -temp2next.Y;
                    break;
                case "OBJ_t0 pelvis":
                    //temp2.X = -temp2.X;//
                    temp2.Y = -temp2.Y;//
                    temp2next.Y = -temp2next.Y;
                    //temp2.Z = -temp2.Z;
                break;
                case "OBJ_t0 footsteps":
                    //temp2.X = -temp2.X;//
                    temp2.Y = -temp2.Y;//
                    temp2next.Y = -temp2next.Y;
                    //temp2.Z = -temp2.Z;
                    break;
                case "OBJ_t0 spine":
                    //temp2.X = -temp2.X;//
                    temp2.Y = -temp2.Y;//
                    temp2next.Y = -temp2next.Y;
                    //temp2.Z = -temp2.Z;
                    break;
                case "OBJ_t0 spine1":
                    //temp2.X = -temp2.X;//
                    temp2.Y = -temp2.Y;//
                    temp2next.Y = -temp2next.Y;
                    //temp2.Z = -temp2.Z;
                    break;
                case "OBJ_t0 neck":
                    //temp2.X = -temp2.X;//
                    temp2.Y = -temp2.Y;//
                    temp2next.Y = -temp2next.Y;
                    //temp2.Z = -temp2.Z;
                    break;
                case "OBJ_t0 head":
                    //temp2.X = -temp2.X;//
                    temp2.Y = -temp2.Y;//
                    temp2next.Y = -temp2next.Y;
                    //temp2.Z = -temp2.Z;
                    break;
                case "OBJ_t0 r clavicle":
                    //temp2.X = -temp2.X;//
                    temp2.Y = -temp2.Y;//
                    temp2next.Y = -temp2next.Y;
                    //temp2.Z = -temp2.Z;
                    break;
                case "OBJ_t0 l clavicle":
                    //temp2.X = -temp2.X;
                    temp2.Y = -temp2.Y;//
                    temp2next.Y = -temp2next.Y;
                    //temp2.Z = -temp2.Z;//
                    break;
                case "OBJ_t0 r upperarm":
                    //temp2.X = -temp2.X;//
                    temp2.Y = -temp2.Y;//
                    temp2next.Y = -temp2next.Y;
                    //temp2.Z = -temp2.Z;
                    break;
                case "OBJ_t0 l upperarm":
                    //temp2.X = -temp2.X;
                    temp2.Y = -temp2.Y;//
                    temp2next.Y = -temp2next.Y;
                    //temp2.Z = -temp2.Z;//
                    break;
                case "OBJ_t0 r forearm":
                    //temp2.X = -temp2.X;//
                    //temp2.X -= Util.toRads(45);
                    temp2.Y = -temp2.Y;//
                    temp2next.Y = -temp2next.Y;
                    //temp2.Z = -temp2.Z;
                    break;
                case "OBJ_t0 l forearm":
                    //temp2.X = -temp2.X;
                    temp2.Y = -temp2.Y;//
                    temp2next.Y = -temp2next.Y;
                    //temp2.Z = -temp2.Z;//
                    break;
                case "OBJ_t0 r hand":
                    //temp2.X = -temp2.X;//
                    temp2.Y = -temp2.Y;//
                    temp2next.Y = -temp2next.Y;
                    //temp2.Z = -temp2.Z;
                    break;
                case "OBJ_t0 l hand":
                    //temp2.X = -temp2.X;
                    temp2.Y = -temp2.Y;//
                    temp2next.Y = -temp2next.Y;
                    //temp2.Z = -temp2.Z;//
                    break;
                case "OBJ_t0 r finger0":
                    //temp2.X = -temp2.X;//
                    temp2.Y = -temp2.Y;//
                    temp2next.Y = -temp2next.Y;
                    //temp2.Z = -temp2.Z;
                    break;
                case "OBJ_t0 l finger0":
                    //temp2.X = -temp2.X;
                    temp2.Y = -temp2.Y;//
                    temp2next.Y = -temp2next.Y;
                    //temp2.Z = -temp2.Z;//
                    break;
                case "OBJ_t0 r thigh":
                    temp2.Y = -temp2.Y;
                    temp2next.Y = -temp2next.Y;
                    //temp2.X = -temp2.X;
                    //temp2.Z = -temp2.Z;
                    break;
                case "OBJ_t0 l thigh":
                    //temp2.Z = -temp2.Z;
                    //temp2.X = -temp2.X;
                    temp2.Y = -temp2.Y;
                    temp2next.Y = -temp2next.Y;
                    break;
                case "OBJ_t0 r calf":
                    temp2.Y = -temp2.Y;
                    temp2next.Y = -temp2next.Y;
                    //temp2.X = -temp2.X;
                    //temp2.Z = -temp2.Z;
                    break;
                case "OBJ_t0 l calf":
                    //temp2.Z = -temp2.Z;
                    ////temp2.X = -temp2.X;
                    temp2.Y = -temp2.Y;
                    temp2next.Y = -temp2next.Y;
                    break;
                case "OBJ_t0 r foot":
                    temp2.Y = -temp2.Y;
                    temp2next.Y = -temp2next.Y;
                    //temp2.X = -temp2.X;
                    //temp2.Z = -temp2.Z;
                    break;
                case "OBJ_t0 l foot":
                    //temp2.Z = -temp2.Z;
                    //temp2.X = -temp2.X;
                    temp2.Y = -temp2.Y;
                    temp2next.Y = -temp2next.Y;
                    break;
                case "OBJ_t0 r belt1":
                    temp2.Y = -temp2.Y;
                    temp2next.Y = -temp2next.Y;
                    break;
                case "OBJ_t0 l belt1":
                    temp2.Y = -temp2.Y;
                    temp2next.Y = -temp2next.Y;
                    break;

            }

            float range;
            if (CurrentValue.GetFrameCount() - CurrentValue.FrameNumber() == 0)
            {
                range = 1.0f;
            }
            else
            {
                range = 1.0f / (CurrentValue.GetFrameCount() - CurrentValue.FrameNumber());
            }
            int percent = frameNumber - CurrentValue.FrameNumber();
            //return Util.Slerp(range * percent, CurrentValue.Value(), NextValue.Value());
            //return Util.V3Slerp(range * percent, CurrentValue.Value(), NextValue.Value());
            //return Util.Slerp(range * percent, temp, temp2);

            //TODO: Fix vector3 slerping
            //return Vector3.Lerp(CurrentValue.Value(), NextValue.Value(), range * percent);

            //return CurrentValue.Value();
            if (KeyCount == 0)
            {
                return temp;
            }
            //else
            //{
                //return CurrentValue.Value();
            //    return temp2;
            //}
            return Vector3.Lerp(temp2, temp2next, range * percent);

        }
    }

    public class Vec4Rotation_Track
    {
        public List<Vec4Key_Rotation> Keys = new List<Vec4Key_Rotation>();
        public Vec4Key_Rotation FixedValue = new Vec4Key_Rotation();
        int KeyCount = 0;
        int CurrentKey = 0;

        public Vec4Key_Rotation GetValue(int keyID)
        {
            if(KeyCount == 0)
            {
                return FixedValue;
            }
            else if(keyID < Keys.Count)
            {
                return Keys[keyID];
            }
            return Keys[Keys.Count - 1];
        }

        public void Read(BinaryReader bStream, int TrackType, int frameCount)
        {
            if(TrackType == CCS_ANIME_CONTROLLER_TYPE_FIXED)
            {
                FixedValue.Read(bStream, 0);
            }
            else if(TrackType == CCS_ANIME_CONTROLLER_TYPE_ANIMATED)
            {
                KeyCount = bStream.ReadInt32();
                for(int i = 0; i < KeyCount; i++)
                {
                    int frameNum = bStream.ReadInt32();
                    Debug.WriteLine("\t\tReading Rotation4 Track Key {0} of {1}, Frame: {2}", i, KeyCount, frameNum);
                    var tmpKey = new Vec4Key_Rotation();
                    tmpKey.Read(bStream, frameNum);
                    if(Keys.Count > 0)
                    {
                        Vec4Key_Rotation lastKeyframe = Keys[Keys.Count - 1];
                        if(lastKeyframe.FrameNumber() == frameNum)
                        {
                            //kill duplicate keyframe...
                            //TODO: Vec4Key_Rotation: Properly handle duplicate keyframes
                            //Keys.Remove(lastKeyframe);
                        }
                    }
                    if(Keys.Count > 0)
                    {
                        int LastKeyID = Keys.Count - 1;
                        Keys[LastKeyID].SetFrameCount(tmpKey.FrameNumber());
                    }
                    Keys.Add(tmpKey);
                }
                int FinalKeyID = Keys.Count - 1;
                Keys[FinalKeyID].SetFrameCount(frameCount);
            }
            //TODO: Vec4Key_Rotation::Read(): Warn about unkown track type.
        }

        public Quaternion GetInterpolatedValue(int frameNumber)
        {
            //Placeholder
            //TODO: Vec4Key_Rotation::GetInterpolatedValue();
            if(KeyCount == 0) return FixedValue.Value();

            if(frameNumber == 0) CurrentKey = 0;
            
            return GetValue(CurrentKey).Value();
        }
    }

    public class Vec3Position_Track
    {
        public List<Vec3Key_Position> Keys = new List<Vec3Key_Position>();
        public Vec3Key_Position FixedValue = new Vec3Key_Position();
        int KeyCount = 0;
        int CurrentKey = 0;

        public Vec3Key_Position GetValue(int keyID)
        {
            if(KeyCount == 0)
            {
                return FixedValue;
            }
            else if(keyID < Keys.Count)
            {
                return Keys[keyID];
            }
            return Keys[Keys.Count - 1];
        }

        public void Read(BinaryReader bStream, int TrackType, int frameCount)
        {
            if(TrackType == CCS_ANIME_CONTROLLER_TYPE_FIXED)
            {
                FixedValue.Read(bStream, 0);
            }
            else if(TrackType == CCS_ANIME_CONTROLLER_TYPE_ANIMATED)
            {
                KeyCount = bStream.ReadInt32();
                for(int i = 0; i < KeyCount; i++)
                {
                    int frameNum = bStream.ReadInt32();
                    Debug.WriteLine("\t\tReading Position Track Key {0} of {1}, Frame: {2}", i, KeyCount, frameNum);
                    var tmpKey = new Vec3Key_Position();
                    tmpKey.Read(bStream, frameNum);
                    if(Keys.Count > 0)
                    {
                        Vec3Key_Position lastKeyframe = Keys[Keys.Count - 1];
                        if(lastKeyframe.FrameNumber() == frameNum)
                        {
                            //kill duplicate keyframe...
                            //TODO: Vec3Position_Track: Properly handle duplicate keyframes
                            //Keys.Remove(lastKeyframe);
                        }
                    }
                    if(Keys.Count > 0)
                    {
                        int LastKeyID = Keys.Count - 1;
                        Keys[LastKeyID].SetFrameCount(tmpKey.FrameNumber());
                    }
                    Keys.Add(tmpKey);
                }
                int FinalKeyID = Keys.Count - 1;
                Keys[FinalKeyID].SetFrameCount(frameCount);
            }
            //TODO: Vec3Position_Track::Read(): Warn about unkown track type.
        }

        public Vector3 GetInterpolatedValue(int frameNumber, Vector3 rotation, string filename)
        {
            //Placeholder
            //TODO: Vec3Position_Track::GetInterpolatedValue();
            //if (KeyCount == 0) return FixedValue.Value();

            if (frameNumber == 0) CurrentKey = 0;

            var CurrentValue = GetValue(CurrentKey);
            if (frameNumber >= CurrentValue.GetFrameCount())
            {
                CurrentKey += 1;
                if (CurrentKey > (Keys.Count - 1)) CurrentKey = 0;
            }
            CurrentValue = GetValue(CurrentKey);

            int NextKey = CurrentKey + 1;
            if (NextKey > (Keys.Count - 1))
            {

                NextKey = 0;
            }
            var NextValue = GetValue(NextKey);

            Vector3 temp = new Vector3(1, 1, 1);
            temp.X = CurrentValue.Value().X;
            temp.Y = CurrentValue.Value().Y;
            temp.Z = CurrentValue.Value().Z;
            if (((Math.Abs(rotation.Z) > 2.85 && Math.Abs(rotation.Z) < 3.15) &&
            (int)Math.Abs(rotation.X) == 1) ||
            ((int)Math.Abs(rotation.Z) == 2 && (int)Math.Abs(rotation.X) == 1))
            {
                //temp.X = -temp.X;
                //temp.Y = -temp.Y;
            }

            Vector3 temp2 = new Vector3(1, 1, 1);
            temp2.X = CurrentValue.Value().X;
            temp2.Y = CurrentValue.Value().Y;
            temp2.Z = CurrentValue.Value().Z;
            if (((Math.Abs(rotation.Z) > 2.85 && Math.Abs(rotation.Z) < 3.15) &&
            (int)Math.Abs(rotation.X) == 1) ||
            ((int)Math.Abs(rotation.Z) == 2 && (int)Math.Abs(rotation.X) == 1))
            {
                //temp2.Y = -temp2.Y;
                //temp2.X = -temp2.X;
                //temp2.Z = -temp2.Z;
            }
            if (((Math.Round(Math.Abs(rotation.Z), 1) == 1.5) || Math.Round(Math.Abs(rotation.Z), 1) == 1.6))
            {
                //temp2.X = -temp2.X;
                //temp2.Y = -temp2.Y;
                //temp2.Z = -temp2.Z;
            }

            switch (filename)
            {
                case "OBJ_trall":
                    //temp2.X = 0;
                    //temp2.Y = 0;
                    //temp2.Z = 0;
                    break;
            }

                    float range = 1.0f / (CurrentValue.GetFrameCount() - CurrentValue.FrameNumber());
            int percent = frameNumber - CurrentValue.FrameNumber();
            //return Util.Slerp(range * percent, CurrentValue.Value(), NextValue.Value());
            //return Util.V3Slerp(range * percent, CurrentValue.Value(), NextValue.Value());

            //TODO: Fix vector3 slerping
            //return Vector3.Lerp(CurrentValue.Value(), NextValue.Value(), range * percent);

            if (KeyCount == 0)
            {
                return temp;
            }
            else
            {
                //return CurrentValue.Value();
                return temp2;
            }
            //return Vector3.Lerp(temp, temp2, range * percent);
            //return CurrentValue.Value();
        }
    }

    public class Vec3Scale_Track
    {
        public List<Vec3Key_Scale> Keys = new List<Vec3Key_Scale>();
        public Vec3Key_Scale FixedValue = new Vec3Key_Scale();
        int KeyCount = 0;
        int CurrentKey = 0;

        public Vec3Key_Scale GetValue(int keyID)
        {
            if(KeyCount == 0)
            {
                return FixedValue;
            }
            else if(keyID < Keys.Count)
            {
                return Keys[keyID];
            }
            return Keys[Keys.Count - 1];
        }

        public void Read(BinaryReader bStream, int TrackType, int frameCount)
        {
            if(TrackType == CCS_ANIME_CONTROLLER_TYPE_FIXED)
            {
                FixedValue.Read(bStream, 0);
            }
            else if(TrackType == CCS_ANIME_CONTROLLER_TYPE_ANIMATED)
            {
                KeyCount = bStream.ReadInt32();
                for(int i = 0; i < KeyCount; i++)
                {
                    int frameNum = bStream.ReadInt32();
                    Debug.WriteLine("\t\tReading Scale Track Key {0} of {1}, Frame: {2}", i, KeyCount, frameNum);
                    var tmpKey = new Vec3Key_Scale();
                    tmpKey.Read(bStream, frameNum);
                    if(Keys.Count > 0)
                    {
                        Vec3Key_Scale lastKeyframe = Keys[Keys.Count - 1];
                        if(lastKeyframe.FrameNumber() == frameNum)
                        {
                            //kill duplicate keyframe...
                            //TODO: Vec3Scale_Track: Properly handle duplicate keyframes
                            //Keys.Remove(lastKeyframe);
                        }
                    }
                    
                    if(Keys.Count > 0)
                    {
                        int LastKeyID = Keys.Count - 1;
                        Keys[LastKeyID].SetFrameCount(tmpKey.FrameNumber());
                    }
                    Keys.Add(tmpKey);
                }
                int FinalKeyID = Keys.Count - 1;
                Keys[FinalKeyID].SetFrameCount(frameCount);
            }
            //TODO: Vec3Scale_Track::Read(): Warn about unkown track type.
        }

        public Vector3 GetInterpolatedValue(int frameNumber, Vector3 position, Vector3 rotation, string filename)
        {
            //if (KeyCount == 0) return FixedValue.Value();

            if (frameNumber == 0) CurrentKey = 0;

            var CurrentValue = GetValue(CurrentKey);
            if (frameNumber >= CurrentValue.GetFrameCount())
            {
                CurrentKey += 1;
                if (CurrentKey > (Keys.Count - 1)) CurrentKey = 0;
            }
            CurrentValue = GetValue(CurrentKey);

            int NextKey = CurrentKey + 1;
            if (NextKey > (Keys.Count - 1))
            {
                NextKey = 0;
            }
            var NextValue = GetValue(NextKey);

            Vector3 temp = new Vector3(1, 1, 1);
            temp.X = CurrentValue.Value().X;
            temp.Y = CurrentValue.Value().Y;
            temp.Z = CurrentValue.Value().Z;
            if (((Math.Abs(temp.Z) > 2.85 && Math.Abs(temp.Z) < 3.15) &&
            (int)Math.Abs(temp.X) == 1) ||
            ((int)Math.Abs(temp.Z) == 2 && (int)Math.Abs(temp.X) == 1))
            {
                temp.Y = -temp.Y;
                //temp.X = -temp.X;
            }
            if (((Math.Round(Math.Abs(rotation.Z), 1) == 1.5) || Math.Round(Math.Abs(rotation.Z), 1) == 1.6) && filename.Contains("OBJ_w"))
            {
                temp.Y = -temp.Y;
                //temp.X = -temp.X;
            }

            Vector3 temp2 = Vector3.Zero;
            temp2.X = CurrentValue.Value().X;
            temp2.Y = CurrentValue.Value().Y;
            temp2.Z = CurrentValue.Value().Z;
            if (((Math.Abs(rotation.Z) > 2.85 && Math.Abs(rotation.Z) < 3.15) &&
            (int)Math.Abs(rotation.X) == 1) ||
            ((int)Math.Abs(rotation.Z) == 2 && (int)Math.Abs(rotation.X) == 1))
            {
                //temp2.Z = -temp2.Z;
                //temp2.Y = -temp2.Y;
                //temp2.X = -temp2.X;
            }
            if (((Math.Round(Math.Abs(rotation.Z), 1) == 1.5) || Math.Round(Math.Abs(rotation.Z), 1) == 1.6))
            {
                //temp2.Z = -temp2.Z;
                //temp2.Y = -temp2.Y;
                //temp2.X = -temp2.X;
            }

            switch (filename)
            {
                case "OBJ_t0 r thigh":
                    //temp2.Y = -temp2.Y;
                    //temp2.X = -temp2.X;
                    //temp2.Z = -temp2.Z;
                    break;
                case "OBJ_t0 l thigh":
                    //temp2.Z = -temp2.Z;
                    //temp2.X = -temp2.X;
                    //temp2.Y = -temp2.Y;
                    break;
                case "OBJ_t0 r clavicle":
                    //temp2.X = -temp2.X;/**/
                    //temp2.Y = -temp2.Y;/**/
                    //temp2.Z = -temp2.Z;
                    //temp.X += Util.toRads(90f);
                    break;
                case "OBJ_t0 l clavicle":
                    //temp2.X = -temp2.X;
                    //temp2.Y = -temp2.Y;/**/
                    //temp2.Z = -temp2.Z;/**/
                    //temp2.X += Util.toRads(90f);
                    break;
                case "OBJ_t0 r upperarm":
                    //temp2.X = -temp2.X;/**/
                    //temp2.Y = -temp2.Y;/**/
                    //temp2.Z = -temp2.Z;
                    //temp.X += Util.toRads(90f);
                    break;
                case "OBJ_t0 l upperarm":
                    //temp2.X = -temp2.X;
                    //temp2.Y = -temp2.Y;/**/
                    //temp2.Z = -temp2.Z;/**/
                    //temp2.X += Util.toRads(90f);
                    break;
                case "OBJ_t0 r forearm":
                    //temp2.X = -temp2.X;/**/
                    //temp2.Y = -temp2.Y;/**/
                    //temp2.Z = -temp2.Z;
                    //temp.X += Util.toRads(90f);
                    break;
                case "OBJ_t0 l forearm":
                    //temp2.X = -temp2.X;
                    //temp2.Y = -temp2.Y;/**/
                    //temp2.Z = -temp2.Z;/**/
                    //temp2.X += Util.toRads(90f);
                    break;
                case "OBJ_t0 r hand":
                    //temp2.X = -temp2.X;/**/
                    //temp2.Y = -temp2.Y;/**/
                    //temp2.Z = -temp2.Z;
                    //temp.X += Util.toRads(90f);
                    break;
                case "OBJ_t0 l hand":
                    //temp2.X = -temp2.X;
                    //temp2.Y = -temp2.Y;/**/
                    //temp2.Z = -temp2.Z;/**/
                    //temp2.X += Util.toRads(90f);
                    break;
                case "OBJ_t0 spine 1":
                    //temp2.X = -temp2.X;
                    //temp2.Y = -temp2.Y;/**/
                    //temp2.Z = -temp2.Z;/**/
                    //temp2.X += Util.toRads(90f);
                    break;
            }

            float range = 1.0f / (CurrentValue.GetFrameCount() - CurrentValue.FrameNumber());
            int percent = frameNumber - CurrentValue.FrameNumber();

            //return CurrentValue.Value();
            //return Vector3.Lerp(CurrentValue.Value(), NextValue.Value(), range * percent);
            //return Vector3.Lerp(temp, temp2, range * percent);
            if (Keys.Count == 1)
            {
                return temp;
            }
            else
            {
                //return CurrentValue.Value();
                return temp2;
            }
        }
    }

    public class Vec2UV_Track
    {
        public List<Vec2Key_UV> Keys = new List<Vec2Key_UV>();
        public Vec2Key_UV FixedValue = new Vec2Key_UV();
        int KeyCount = 0;
        int CurrentKey = 0;

        public Vec2Key_UV GetValue(int keyID)
        {
            if(KeyCount == 0)
            {
                return FixedValue;
            }
            else if(keyID < Keys.Count)
            {
                return Keys[keyID];
            }
            return Keys[Keys.Count - 1];
        }

        public void Read(BinaryReader bStream, int TrackType, int frameCount)
        {
            if(TrackType == CCS_ANIME_CONTROLLER_TYPE_FIXED)
            {
                FixedValue.Read(bStream, 0);
            }
            else if(TrackType == CCS_ANIME_CONTROLLER_TYPE_ANIMATED)
            {
                KeyCount = bStream.ReadInt32();
                for(int i = 0; i < KeyCount; i++)
                {
                    int frameNum = bStream.ReadInt32();
                    Debug.WriteLine("\t\tReading Texture Offset Track Key {0} of {1}, Frame: {2}", i, KeyCount, frameNum);
                    var tmpKey = new Vec2Key_UV();
                    tmpKey.Read(bStream, frameNum);
                    if(Keys.Count > 0)
                    {
                        Vec2Key_UV lastKeyframe = Keys[Keys.Count - 1];
                        if(lastKeyframe.FrameNumber() == frameNum)
                        {
                            //kill duplicate keyframe...
                            //TODO: Vec2UV_Track: Properly handle duplicate keyframes
                            //Keys.Remove(lastKeyframe);
                        }
                    }
                    
                    if(Keys.Count > 0)
                    {
                        int LastKeyID = Keys.Count - 1;
                        Keys[LastKeyID].SetFrameCount(tmpKey.FrameNumber());
                    }
                    Keys.Add(tmpKey);
                }
                int FinalKeyID = Keys.Count - 1;
                Keys[FinalKeyID].SetFrameCount(frameCount);
            }
            //TODO: Vec2UV_Track::Read(): Warn about unkown track type.
        }

        public Vector2 GetInterpolatedValue(int frameNumber)
        {
            //Placeholder
            //TODO: Vec2UV_Track::GetInterpolatedValue();
            if(KeyCount == 0) return FixedValue.Value();

            if(frameNumber == 0) CurrentKey = 0;
            
            return GetValue(CurrentKey).Value();
        }
    }

    public class F32_Track
    {
        public List<F32Key> Keys = new List<F32Key>();
        public F32Key FixedValue = new F32Key();
        int KeyCount = 0;
        int CurrentKey = 0;

        public F32Key GetValue(int keyID)
        {
            if(KeyCount == 0)
            {
                return FixedValue;
            }
            else if(keyID < Keys.Count)
            {
                return Keys[keyID];
            }
            return Keys[Keys.Count - 1];
        }

        public void Read(BinaryReader bStream, int TrackType, int frameCount)
        {
            if(TrackType == CCS_ANIME_CONTROLLER_TYPE_FIXED)
            {
                FixedValue.Read(bStream, 0);
            }
            else if(TrackType == CCS_ANIME_CONTROLLER_TYPE_ANIMATED)
            {
                KeyCount = bStream.ReadInt32();
                for(int i = 0; i < KeyCount; i++)
                {
                    int frameNum = bStream.ReadInt32();
                    Debug.WriteLine("\t\tReading Float Track Key {0} of {1}, Frame: {2}", i, KeyCount, frameNum);
                    var tmpKey = new F32Key();
                    tmpKey.Read(bStream, frameNum);
                    if(Keys.Count > 0)
                    {
                        F32Key lastKeyframe = Keys[Keys.Count - 1];
                        if(lastKeyframe.FrameNumber() == frameNum)
                        {
                            //kill duplicate keyframe...
                            //TODO: F32_Track: Properly handle duplicate keyframes
                            //Keys.Remove(lastKeyframe);
                        }
                    }
                    if(Keys.Count > 0)
                    {
                        int LastKeyID = Keys.Count - 1;
                        Keys[LastKeyID].SetFrameCount(tmpKey.FrameNumber());
                    }
                    Keys.Add(tmpKey);
                }
                int FinalKeyID = Keys.Count - 1;
                Keys[FinalKeyID].SetFrameCount(frameCount-1);
            }
            //TODO: F32_Track::Read(): Warn about unkown track type.
        }

        public float GetInterpolatedValue(int frameNumber)
        {
            //Placeholder
            //TODO: F32_Track::GetInterpolatedValue();
            if(KeyCount == 0) return FixedValue.Value();

            if(frameNumber == 0) CurrentKey = 0;
            
            return GetValue(CurrentKey).Value();
        }
        
        public float GetNonInterpolatedValue(int frameNumber)
        {
        	if(KeyCount == 0) return FixedValue.Value();
        	
        	//Fix for broken UV animation that for some reason ends before it should...
        	if(frameNumber == 0) CurrentKey = 0;
        	
        	var CurrentValue = GetValue(CurrentKey);
        	if(frameNumber >= CurrentValue.GetFrameCount())
        	{
        		CurrentKey += 1;
        		if(CurrentKey > (Keys.Count - 1)) CurrentKey = 0;
        	}
        	CurrentValue = GetValue(CurrentKey);
        	
        	return CurrentValue.Value();
        }
    }

    public class Int32_Track
    {
        public List<Int32Key> Keys = new List<Int32Key>();
        public Int32Key FixedValue = new Int32Key();
        int KeyCount = 0;
        int CurrentKey = 0;

        public Int32Key GetValue(int keyID)
        {
            if(KeyCount == 0)
            {
                return FixedValue;
            }
            else if(keyID < Keys.Count)
            {
                return Keys[keyID];
            }
            return Keys[Keys.Count - 1];
        }

        public void Read(BinaryReader bStream, int TrackType, int frameCount)
        {
            if(TrackType == CCS_ANIME_CONTROLLER_TYPE_FIXED)
            {
                FixedValue.Read(bStream, 0);
            }
            else if(TrackType == CCS_ANIME_CONTROLLER_TYPE_ANIMATED)
            {
                KeyCount = bStream.ReadInt32();
                for(int i = 0; i < KeyCount; i++)
                {
                    int frameNum = bStream.ReadInt32();
                    Debug.WriteLine("\t\tReading Int Track Key {0} of {1}, Frame: {2}", i, KeyCount, frameNum);
                    var tmpKey = new Int32Key();
                    tmpKey.Read(bStream, frameNum);
                    if(Keys.Count > 0)
                    {
                        Int32Key lastKeyframe = Keys[Keys.Count - 1];
                        if(lastKeyframe.FrameNumber() == frameNum)
                        {
                            //kill duplicate keyframe...
                            //TODO: Int32_Track: Properly handle duplicate keyframes
                            //Keys.Remove(lastKeyframe);
                        }
                    }
                    if(Keys.Count > 0)
                    {
                        int LastKeyID = Keys.Count - 1;
                        Keys[LastKeyID].SetFrameCount(tmpKey.FrameNumber());
                    }
                    Keys.Add(tmpKey);
                }
                int FinalKeyID = Keys.Count - 1;
                Keys[FinalKeyID].SetFrameCount(frameCount);
            }
            //TODO: Int32_Track::Read(): Warn about unkown track type.
        }

        public int GetInterpolatedValue(int frameNumber)
        {
            //Placeholder
            //TODO: Int32_Track::GetInterpolatedValue();
            if(KeyCount == 0) return FixedValue.Value();
            
            if(frameNumber == 0) CurrentKey = 0;

            return GetValue(CurrentKey).Value();
        }
    }

#endregion

	}
}
