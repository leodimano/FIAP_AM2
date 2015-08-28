using UnityEngine;
using System.Collections;
using System.Linq;

public class pg_Util
{

	public static Color ColorWithString(string value)
	{
		string valid = "01234567890.,";
        value = new string(value.Where(c => valid.Contains(c)).ToArray());
        string[] rgba = value.Split(',');
        
        // BRIGHT pink
        if(rgba.Length < 4)
        	return new Color(1f, 0f, 1f, 1f);

		return new Color(
			float.Parse(rgba[0]),
			float.Parse(rgba[1]),
			float.Parse(rgba[2]),
			float.Parse(rgba[3]));
	}
}

public static class PGExtensions
{
	public static bool Contains(this Transform[] t_arr, Transform t)
	{
		for(int i = 0; i < t_arr.Length; i++)
			if(t_arr[i] == t)
				return true;
		return false;
	}
}
