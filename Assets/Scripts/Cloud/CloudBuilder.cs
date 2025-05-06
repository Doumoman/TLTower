using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    [Header("Properties")]
    public float maxConnectDistance = 2f;
    public float springFrequency = 6f;
    public float springDamping = 0.5f;
    public float mass = 1f;
    public float drag = 0.5f;
    public float angularDrag = 0.5f;
    public float gravityScale = 0.3f;

    void Start(){
        int connectionIndex = 0;

        var children = new List<Rigidbody2D>(GetComponentsInChildren<Rigidbody2D>());

        for (int i = 0; i<children.Count; i++){
            var a = children[i];
            a.mass = mass;
            a.drag = drag;
            a.angularDrag = angularDrag;
            a.gravityScale = gravityScale;

            for(int j=i+1; j<children.Count; j++){
                var b = children[j];
                float d= Vector2.Distance(a.position, b.position);
                if(d<=maxConnectDistance){
                    
                    var joint = a.gameObject.AddComponent<SpringJoint2D>();
                    joint.connectedBody = b;

                    joint.autoConfigureDistance = true;
                    joint.distance = d;
                    joint.frequency = springFrequency;
                    joint.dampingRatio = springDamping;

                    connectionIndex++;
                }
            }
        }
    }
}
