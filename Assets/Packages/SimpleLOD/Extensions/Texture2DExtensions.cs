﻿/* SimpleLOD 1.6     */
/* By Orbcreation BV */
/* Richard Knol      */
/* Mar 11, 2016      */

/* Note: if you also use other packages by Orbcreation,  */
/* you may end up with multiple copies of this file.     */
/* In that case, better delete/merge those files into 1. */

using UnityEngine;
using System;
using System.Collections;

namespace OrbCreationExtensions
{
	public static class Texture2DExtensions
    {
		public static Texture2D GetCopy(this Texture2D tex) {
			return GetCopy(tex, 0, 0, tex.width, tex.height, (tex.mipmapCount > 1));
		}
		public static Texture2D GetCopy(this Texture2D tex, int x, int y, int w, int h) {
			return GetCopy(tex, x, y, w, h, (tex.mipmapCount > 1));
		}
		public static Texture2D GetSection(this Texture2D tex, int x, int y, int w, int h) {
			return GetCopy(tex, x, y, w, h, (tex.mipmapCount > 1));
		}
		public static Texture2D GetCopy(this Texture2D tex, int x, int y, int w, int h, bool mipMaps) {
			Texture2D newTex = new Texture2D(w, h, GetWritableFormat(tex.format), mipMaps);
			newTex.SetPixels(tex.GetPixels(x, y, w, h, 0), 0);
			newTex.Apply(mipMaps, false);
			return newTex;
		}
		public static Texture2D ScaledCopy(this Texture2D tex, int width, int height, bool mipMaps) {
			if(width<=0 || height<=0) return null;
			if(width==tex.width && height==tex.height) return GetCopy(tex, 0, 0, tex.width, tex.height, mipMaps);
			Color[] newPixels = ScaledPixels(tex.GetPixels(0), tex.width, tex.height, width, height);

			Texture2D newTex = new Texture2D(width, height, GetWritableFormat(tex.format), mipMaps);
			newTex.SetPixels(newPixels,0);
			newTex.Apply(mipMaps, false);
			return newTex;
		}
		public static void CopyFrom(this Texture2D tex, Texture2D fromTex, int toX, int toY, int fromX, int fromY, int width, int height) {
			MakeFormatWritable(tex);
			int fullWidth = tex.width;
			Color[] pixels = tex.GetPixels(0);
			Color[] fromPixels = fromTex.GetPixels(fromX, fromY, width, height, 0);
			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					pixels[((y + toY) * fullWidth) + x + toX] = fromPixels[(y * width) + x];
				}
			}
			tex.SetPixels(pixels,0);
			tex.Apply((tex.mipmapCount > 1), false);
		}

		public static void Scale(this Texture2D tex, int width, int height) {
			if(width<=0 || height<=0 || (width==tex.width && height==tex.height)) return;
			MakeFormatWritable(tex);
			Color[] newPixels = ScaledPixels(tex.GetPixels(0), tex.width, tex.height, width, height);
			if(tex.Resize(width, height, tex.format, tex.mipmapCount>1)) {
				tex.SetPixels(newPixels,0);
				tex.Apply((tex.mipmapCount > 1), false);
			}
		}

		public static void MakeFormatWritable(this Texture2D tex) {
			TextureFormat oldFormat = tex.format;
			TextureFormat newFormat = GetWritableFormat(tex.format);
			if(newFormat != oldFormat) {
				Color[] pixels = tex.GetPixels(0);
				tex.Resize(tex.width, tex.height, newFormat, tex.mipmapCount>1);
				tex.SetPixels(pixels,0);
				tex.Apply((tex.mipmapCount > 1), false);
			}
		}

		public static TextureFormat GetWritableFormat(TextureFormat format) {
			if(format != TextureFormat.Alpha8 && format != TextureFormat.RGB24 && format != TextureFormat.ARGB32 && format != TextureFormat.RGBA32) {
				if(format == TextureFormat.RGB24 || format == TextureFormat.DXT1 || format == TextureFormat.PVRTC_RGB2 || format == TextureFormat.PVRTC_RGB4 || format == TextureFormat.ETC_RGB4 || format == TextureFormat.ETC_RGB4 || format == TextureFormat.ETC2_RGBA8 || format == TextureFormat.ETC2_RGB || format == TextureFormat.ASTC_RGB_4x4 || format == TextureFormat.ASTC_RGB_5x5 || format == TextureFormat.ASTC_RGB_5x5 || format == TextureFormat.ASTC_RGB_5x5 || format == TextureFormat.ASTC_RGB_6x6 || format == TextureFormat.ASTC_RGB_10x10 || format == TextureFormat.ASTC_RGB_12x12) format = TextureFormat.RGB24;
				else format = TextureFormat.RGBA32;
			}
			return format;
		}		

		public static Color GetAverageColor(this Texture2D tex) {
    		Vector4 c = Vector4.zero;
    		float tot = 0f;
    		Color[] pxls = tex.GetPixels(0);
    		for(int i=0;i<pxls.Length;i++) {
        		c += ((Vector4)pxls[i]) * pxls[i].a;
        		tot += pxls[i].a;
        	}
        	if(tot < 1f) tot = 1f;
        	c.w = tot;
        	return (Color)(c / tot);
        }

		public static Color GetAverageColor(this Texture2D tex, Color useThisColorForAlpha) {
    		Vector4 c = Vector4.zero;
    		float tot = 0f;
    		Color[] pxls = tex.GetPixels(0);
    		for(int i=0;i<pxls.Length;i++) {
        		c += ((Vector4)pxls[i]) * pxls[i].a;
        		c += ((Vector4)useThisColorForAlpha) * (1f - pxls[i].a);
        		tot += 1f;
        	}
        	if(tot < 1f) tot = 1f;
        	c.w = tot;
        	return (Color)(c / tot);
        }

	    public static bool IsReadable(this Texture2D tex) {
	    	try {
		        tex.GetPixels(0, 0, 1, 1, 0);
	        } catch(Exception e) {
	        	return e == null;
	        }
	        return true;
	    }


	    public static bool HasTransparency(this Texture2D tex) {
	    	Color[] pixels;
	    	try {
		        pixels = tex.GetPixels(0);
	        } catch(Exception e) {
	        	Debug.LogError(e);
	        	return false;
	        }
	        for (int i=0; i<pixels.Length; i++) {
	        	if(pixels[i].a < 1f) return true;
	        }
	        return false;
	    }
	    
	    public static bool IsNormalMap(this Texture2D aTexture) {
	        Color avgColor = aTexture.GetAverageColor();
	        float rg = (avgColor.r + avgColor.g) / 2f;
	        if(rg <= 0f) rg = 1f;
	        float relativeBlue = avgColor.b / rg;
//	          Debug.Log("avgColor:"+avgColor+" blue/rg "+relativeBlue);
	        //  tested the average color of few normalmaps and images with a lot of blue in it
	        //  normalmap1  avgColor:RGBA(0.407, 0.405, 0.796, 1.000) blue/rg 1.958531
	        //  normalmap2  avgColor:RGBA(0.498, 0.498, 0.995, 1.000) blue/rg 1.999087
	        //  normalmap3  avgColor:RGBA(0.502, 0.502, 0.994, 1.000) blue/rg 1.98113
	        //  blueimage1  avgColor:RGBA(0.360, 0.577, 0.770, 1.000) blue/rg 1.643365
	        //  blueimage2  avgColor:RGBA(0.234, 0.340, 0.561, 1.000) blue/rg 1.953209
	        //  blueimage3  avgColor:RGBA(0.371, 0.659, 0.869, 1.000) blue/rg 1.688144
	        //  blueimage4  avgColor:RGBA(0.529, 0.681, 0.967, 1.000) blue/rg 1.59855
	        //  typically normal maps have lots of blue, when compared to red and green
	        //  red and green have almost identical values
	        //  So I will use 2 criteria:
	        //          1: relative blue value > 1.8 (blue / avg(red and green))
	        //          2: difference between red and green < 0.1
	        if(relativeBlue > 1.8f && Mathf.Abs(avgColor.r - avgColor.g) < 0.1f) return true;
	        return false;
	    }

		private static Color[] ScaledPixels(Color[] originalPixels, int oldWidth, int oldHeight, int width, int height) {
			if(width<=0 || height<=0 || (width==oldWidth && height==oldHeight)) return originalPixels;
			float scaleX = (float)width / (float)oldWidth;
			float scaleY = (float)height / (float)oldHeight;
			Color[] newPixels = new Color[width * height];
			for (int y = 0; y < height; y++) {
				float originalY = y / scaleY;
				int originalYLow = Mathf.FloorToInt(originalY);
				int originalYHigh = Mathf.CeilToInt(originalY);
				for (int x = 0; x < width; x++) {
					float originalX = x / scaleX;
					int originalXLow = Mathf.FloorToInt(originalX);
					int originalXHigh = Mathf.CeilToInt(originalX);
					Color pixel = originalPixels[(originalYLow * oldWidth) + originalXLow] * 
							(1.0f - (originalY - originalYLow)) * 
							(1.0f - (originalX - originalXLow));
					if(originalXLow < originalXHigh && originalXHigh<oldWidth) {
						pixel = pixel + originalPixels[(originalYLow * oldWidth) + originalXHigh] * 
							(1.0f - (originalY - originalYLow)) * 
							(1.0f - (originalXHigh - originalX));
					}
					if(originalYLow < originalYHigh && originalYHigh<oldHeight) {
						pixel = pixel + originalPixels[(originalYHigh * oldWidth) + originalXLow] * 
							(1.0f - (originalYHigh - originalY)) * 
							(1.0f - (originalX - originalXLow));
						if(originalXLow < originalXHigh && originalXHigh<oldWidth) {
							pixel = pixel + originalPixels[(originalYHigh * oldWidth) + originalXHigh] * 
								(1.0f - (originalYHigh - originalY)) * 
								(1.0f - (originalXHigh - originalX));
						}
					}
					newPixels[(y * width) + x] = pixel;
				}
			}
			return newPixels;
		}

	    public static Texture2D GetUnityNormalMap(this Texture2D tex) {
	        Texture2D normalTexture = new Texture2D(tex.width, tex.height, TextureFormat.ARGB32, tex.mipmapCount > 1);
	        Color[] pixels = tex.GetPixels(0);
	        Color[] nPixels = new Color[pixels.Length];
	        for (int y=0; y<tex.height; y++) {
	            for (int x=0; x<tex.width; x++) {
	                Color p = pixels[(y * tex.width) + x];
	                Color np = new Color(0,0,0,0);
	                np.r = p.g;
	                np.g = p.g; // waste of memory space if you ask me
	                np.b = p.g;
	                np.a = p.r;  
	                nPixels[(y * tex.width) + x] = np;
	            }
	        }
	        normalTexture.SetPixels(nPixels, 0);
	        normalTexture.Apply(tex.mipmapCount > 1, false);
	        return normalTexture;
	    }
	    public static Texture2D FromUnityNormalMap(this Texture2D tex) {
	        Texture2D normalTexture = new Texture2D(tex.width, tex.height, TextureFormat.RGB24, tex.mipmapCount > 1);
	        Color[] pixels = tex.GetPixels(0);
	        Color[] nPixels = new Color[pixels.Length];
	        for (int y=0; y<tex.height; y++) {
	            for (int x=0; x<tex.width; x++) {
	                Color p = pixels[(y * tex.width) + x];
	                Color np = new Color(0,0,0,0);
	                np.g = p.r;
	                np.r = p.a;
	                np.b = 1f;
	                nPixels[(y * tex.width) + x] = np;
	            }
	        }
	        normalTexture.SetPixels(nPixels, 0);
	        normalTexture.Apply(tex.mipmapCount > 1, false);
	        return normalTexture;
	    }
	    public static void Fill(this Texture2D tex, Color aColor) {
			MakeFormatWritable(tex);
	        Color[] pixels = tex.GetPixels(0);
	        for (int i=0; i<pixels.Length; i++) {
                pixels[i] = aColor;
	        }
	        tex.SetPixels(pixels, 0);
	        tex.Apply(tex.mipmapCount > 1, false);
		}

	}
}


