using System.Collections;
using UnityEngine;

// The secret card (digital only). When an event flip turns up the secret card (QuakerBalance: secretCardOn,
// secretCardChance), a giant rubber duck (QuakerBalance: secretCardProp, Prop_RubberDuck) flies across the table
// straight through the tower and knocks every block off the tower base. The tower has collapsed, so the game ends
// the usual way (R-16): whoever placed the most recent block loses. Blocks it knocks off are never strikes (R-27).
public partial class QuakerPlay
{
    const float DuckLengthInCells = 3.5f;   // the duck is about 3.5 blocks long: big enough to clear the 4 x 4 base
    const float DuckSpeed = 28f;            // units per second
    const float DuckRunUp = 34f;            // how far from the tower it starts and ends

    IEnumerator SecretCardStrike()
    {
        busy = true;
        string title = s.eventCard != null ? s.eventCard.title : "Secret card";
        Add("  SECRET CARD: " + title + " flies through the tower.");
        QueueBanner("Secret card: " + title + "!", "It flies straight through the tower. The whole tower comes down.", new Color(1f, 0.85f, 0.2f));
        yield return new WaitForSeconds(1.2f);   // let everyone read the card

        // Fly across the screen, left to right as the players see it, at the height of the bottom of the tower.
        Vector3 dir = Camera.main != null ? Camera.main.transform.right : Vector3.right;
        dir.y = 0f;
        dir = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.right;

        GameObject duck = SpawnSecretProp();
        duck.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);   // the duck's beak points along +z
        Bounds db = RenderBounds(duck);
        Vector3 lift = Vector3.up * (baseTop + 0.3f - db.min.y);              // its belly just above the tower base
        Vector3 from = baseCentre - dir * DuckRunUp, to = baseCentre + dir * DuckRunUp;
        Vector3 offset = duck.transform.position - db.center;                 // move by its middle, not its pivot
        offset.y = 0f;
        duck.transform.position = new Vector3(from.x, duck.transform.position.y, from.z) + offset + lift;

        Rigidbody body = duck.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        // The flight: straight through, with a little bob and waddle.
        Vector3 start = duck.transform.position, end = start + (to - from);
        Quaternion facing = duck.transform.rotation;
        float seconds = (to - from).magnitude / DuckSpeed, t = 0f;
        bool hit = false;
        while (t < seconds)
        {
            yield return new WaitForFixedUpdate();
            t += Time.fixedDeltaTime;
            float k = Mathf.Clamp01(t / seconds);
            Vector3 p = Vector3.Lerp(start, end, k) + Vector3.up * (Mathf.Sin(t * 9f) + 1f) * 0.3f;   // bobs up, never into the base
            body.MovePosition(p);
            body.MoveRotation(facing * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 12f) * 8f));
            if (!hit && k >= 0.4f) { hit = true; GameSounds.Play(GameSound.Collapse); }
        }
        Destroy(duck);

        // Anything the duck missed is swept off the same way, until nothing is left on the tower base.
        for (int pass = 0; pass < 4; pass++)
        {
            bool any = false;
            foreach (Rigidbody rb in placed)
            {
                if (rb == null || !OnTower(rb)) continue;
                any = true;
                rb.AddForce(dir * 14f + Vector3.up * 5f + Random.insideUnitSphere * 2f, ForceMode.VelocityChange);
                rb.AddTorque(Random.insideUnitSphere * 8f, ForceMode.VelocityChange);
            }
            if (!any) break;
            yield return new WaitForSeconds(1.2f);
        }

        // The usual check: every block is off the base, so the tower has collapsed (R-16).
        yield return SettleThenCheck(false);
    }

    // The prop from QuakerBalance, scaled up so it is about DuckLengthInCells blocks long, with one box collider.
    GameObject SpawnSecretProp()
    {
        GameObject prefab = B.secretCardProp;
        GameObject model = prefab != null ? Instantiate(prefab) : GameObject.CreatePrimitive(PrimitiveType.Sphere);
        model.name = "Secret card: " + (prefab != null ? prefab.name : "ball");
        foreach (Collider c in model.GetComponentsInChildren<Collider>()) Destroy(c);

        Bounds b = RenderBounds(model);
        float longest = Mathf.Max(b.size.x, b.size.z, 0.01f);
        model.transform.localScale *= cell * DuckLengthInCells / longest;

        // One solid box around the whole duck, so it pushes blocks the way you'd expect.
        Bounds world = RenderBounds(model);
        BoxCollider box = model.AddComponent<BoxCollider>();
        box.center = model.transform.InverseTransformPoint(world.center);
        Vector3 ls = model.transform.lossyScale;
        box.size = new Vector3(world.size.x / ls.x, world.size.y / ls.y, world.size.z / ls.z);
        return model;
    }
}
