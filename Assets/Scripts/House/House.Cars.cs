using System.Collections.Generic;
using UnityEngine;

// House (partial) - The parked vehicles of a lot: cars in a yard and in a car park. They are LowPolyCar (see LowPolyCar.cs), pooled per lot.
// The main part of the class is in House.cs.
public partial class House
{
    readonly List<LowPolyCar> cars = new List<LowPolyCar>();
    int carN;

    // Puts a vehicle at (x, z) on the ground. Returns the one placed (its type is in 'type').
    void PlaceCar(float x, float z, int type, int colour, float maxLen)
    {
        if (carN >= cars.Count) cars.Add(LowPolyCar.Create(transform, "Vehicle" + cars.Count));
        float yaw = Random.value < 0.5f ? 0f : 180f;
        cars[carN++].Place(new Vector3(x, 0f, z), yaw, type, colour, maxLen);
        float len = Mathf.Min(maxLen, LowPolyCar.Length(type));
        Blob(new Vector3(x, 0f, z), LowPolyCar.Width(type) + 0.7f, len + 0.7f, 0.3f);
    }

    void HideCarsFrom(int from)
    {
        for (int i = from; i < cars.Count; i++) cars[i].Hide();
    }
}
