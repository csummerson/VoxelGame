using Godot;
using System;

public partial class SphericalCoordinateDisplay : Label
{
	[Export] Camera3D cam;
	[Export] int seaLevel = 0;

	public override void _Ready()
	{
		cam = GetViewport().GetCamera3D();
		seaLevel = GameManager.Instance.SIZE * 16;
	}


	/*
	rho: radius
	theta: azimuth
	phi: zenith
	*/

	public override void _Process(double delta)
	{
		// IMPORTANT! REMEMBER GODOT HAS Y AS UP AND XZ PLANE AS GROUND!!!
		// +X is RIGHT
		// -Z is FORWARD
		// +Y is UP
		// FOR ALL ONLINE FORMULAS, SUBSTITUTE AND NEGATE AS NEEDED!

		Vector3 p = cam.GlobalPosition;
		string worldCoord = $"x: {p.X:F2} y: {p.Y:F2} z: {p.Z:F2}";

		float rho = Mathf.Sqrt(Mathf.Pow(p.X, 2) + Mathf.Pow(p.Y, 2) + Mathf.Pow(p.Z, 2));
		float theta = Mathf.Atan2(p.X, -p.Z); // azimuth
		float phi = Mathf.Acos(p.Y / rho); //zenith
		string sphericalCoord = $"ρ: {rho:F2} θ: {theta:F2} φ: {phi:F2}";

		float latitudeVal = Mathf.RadToDeg(phi);
		float longitudeVal = -Mathf.RadToDeg(theta);

		string latitude;
		string longitude;
		float alt = rho - seaLevel;

		if (latitudeVal < 90)
		{
			latitude = $"{Mathf.Abs(latitudeVal - 90):F2}°N";
		}
		else
		{
			latitude = $"{Mathf.Abs(90 - latitudeVal):F2}°S";
		}

		if (longitudeVal > 0)
		{
			longitude = $"{longitudeVal:F2}°E";
		}
		else
		{
			longitude = $"{Mathf.Abs(longitudeVal):F2}°W";
		}

		string gps = $"GPS: {latitude} {longitude} {alt:F2}m";

		string chunk = "Chunk: " + GameManager.Instance.chunkInfo;
		string seed = "Seed: " + GameSettings.Instance.seed;

		Text = worldCoord + "\n" + gps + "\n" + sphericalCoord + "\n" + chunk + "\n" + seed;

		
	}
}
