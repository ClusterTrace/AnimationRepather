// Programmer: Trace
// Date: November 8, 2022
// Purpose: This is an editor script to help edit the paths of animation clips in Unity Game Engine


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// Can find this window by clicking Clip Info under the Window tab (should be to the right of File somewhere)
public class AnimationRepather : EditorWindow
{
    private AnimationClip clip;
    private GameObject newRootObject;
    private GameObject optionalParent;
    private GameObject fillObject;
    private bool showHelp = false;
    private Vector2 scrollPos1;
    //private Vector2 scrollPos2;
    private Vector2 scrollPos3;

    [MenuItem("Window/Anim Clip Repath")]

    static void Init()
    {
        GetWindow(typeof(AnimationRepather));
    }

    public void OnGUI()
    {
        clip = EditorGUILayout.ObjectField("Animation Clip", clip, typeof(AnimationClip), true) as AnimationClip;
        newRootObject = EditorGUILayout.ObjectField("New Root Object", newRootObject, typeof(GameObject), true) as GameObject;
        optionalParent = EditorGUILayout.ObjectField("Optional Parent", optionalParent, typeof(GameObject), true) as GameObject;
        fillObject = EditorGUILayout.ObjectField("Fill Object", fillObject, typeof(GameObject), true) as GameObject;
        //rootName = EditorGUILayout.TextField("Root Name", rootName) as string;
        bool pressedRebindCurves = GUILayout.Button("Rebind Curves!"); // makes a button that returns true when pressed
        bool pressedRebindObjectCurves = GUILayout.Button("Rebind Object Reference Curves!"); // makes a button that returns true when pressed
        bool pressedClearPaths = GUILayout.Button("Clear Paths"); // makes a button that returns true when pressed
        bool pressedInstructionsToggle = GUILayout.Button("Instructions/Notes toggle"); // makes a button that returns true when pressed



        if (pressedInstructionsToggle)
        {
            showHelp = !showHelp;
        }

        if (showHelp)
        {
            showInstructions();
        }
        else
        {
            if (clip != null)
            {
                // edits curves if button pressed
                if (pressedRebindCurves)
                {
                    updateAnimationProperties(0); // calls it for animation curves
                }
                else if (pressedRebindObjectCurves)
                {
                    updateAnimationProperties(1); // calls it for object reference curves
                }
                else if (pressedClearPaths)
                {
                    clearPaths();
                }

                // displays info about curves
                GUILayout.Label("Curves:------------------------------------");
                scrollPos1 = EditorGUILayout.BeginScrollView(scrollPos1, GUILayout.Width(300), GUILayout.Height(300));
                UnityEditor.EditorCurveBinding[] bindings1 = AnimationUtility.GetCurveBindings(clip);
                for (int i = 0; i < bindings1.Length; i++)
                {
                    var binding = bindings1[i];
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                    GUILayout.Label(binding.path + "/" + binding.propertyName + ", Keys: " + curve.keys.Length);
                }

                GUILayout.Label("Object reference curves:--------------------");
                UnityEditor.EditorCurveBinding[] bindings2 = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                for (int i = 0; i < bindings2.Length; i++)
                {
                    var binding = bindings2[i];

                    ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                    GUILayout.Label(binding.path + "/" + binding.propertyName + ", Keys: " + keyframes.Length);
                }
                EditorGUILayout.EndScrollView();
            }
            else
            {
                GUILayout.Label("No Clip Selected...");
            }
        }
    }

    private void clearPaths()
    {
        UnityEditor.EditorCurveBinding[] bindings1 = AnimationUtility.GetCurveBindings(clip);
        UnityEditor.EditorCurveBinding[] bindings2 = AnimationUtility.GetObjectReferenceCurveBindings(clip);

        for (int i = 0; i < bindings1.Length; i++)
        {
            var bindingTemp = bindings1[i];
            string tempPath = bindingTemp.path;
            AnimationCurve tempCurve = AnimationUtility.GetEditorCurve(clip, bindingTemp);
            bindingTemp.path = ""; // sets the new path for the curve
            AnimationUtility.SetEditorCurve(clip, bindingTemp, tempCurve); // adds curve with new path
            if (bindingTemp.path != tempPath) // prevents removing curve if path was the same, as setting earlier wouldn't have added a new curve
            {
                bindingTemp.path = tempPath;
                AnimationUtility.SetEditorCurve(clip, bindingTemp, null); // removes original curve
            }
        }
        for (int i = 0; i < bindings2.Length; i++)
        {
            var bindingTemp = bindings2[i];
            string tempPath = bindingTemp.path;
            ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(clip, bindingTemp);
            bindingTemp.path = ""; // sets the new path for the curve
            AnimationUtility.SetObjectReferenceCurve(clip, bindingTemp, keyframes); // adds curve with new path
            if (bindingTemp.path != tempPath) // prevents removing curve if path was the same, as setting earlier wouldn't have added a new curve
            {
                bindingTemp.path = tempPath;
                AnimationUtility.SetObjectReferenceCurve(clip, bindingTemp, null); // removes original curve
            }
        }
    }

    private void updateAnimationProperties(int typeOfUpdate)
    {
        ArrayList paths = new ArrayList();
        bool makingPath = true;

        UnityEditor.EditorCurveBinding[] editedBindings = AnimationUtility.GetCurveBindings(clip);
        int bindingCount = editedBindings.Length;
        for (int i = 0; i < bindingCount; i++)
        {
            var bindingTemp = editedBindings[i];
            string tempPath = bindingTemp.path;
            if (makingPath) // has to make the new paths before updating old ones
            {
                string newPath = "";
                if (tempPath.Length <= 0 && fillObject != null) // adds the fill object to empty path
                {
                    newPath = fillObject.name;
                }
                if (tempPath.Length > 0) // edits existing path
                {
                    int tempIndex = 0;
                    // finds the index for what would be the bone
                    for (int j = tempPath.Length; j > 0; j--)
                    {
                        if (tempPath[j - 1] == '/') // stops it from going farther than the bone
                        {
                            tempIndex = j;
                            j = 0; // forces the for loop to the end, stopping it
                        }
                    }
                    // sets newPath to the bone
                    newPath = tempPath.Substring(tempIndex);
                    // finds the bone in the scene
                    GameObject currentRoot = GameObject.Find(newPath);
                    if (!currentRoot.transform.IsChildOf(newRootObject.transform)) // checks if found the child as a gameObject (could accidentally find a duplicate)
                    {
                        // tries to find the object using the optional parent
                        // uses the optional unique parent to recursively search for the correct child
                        if (optionalParent != null)
                            currentRoot = findChildRecursive(optionalParent.transform, newPath).gameObject;
                        if (currentRoot == null)
                            Debug.Log("Unable to find child using optional parent.");
                    }

                    // adds all of the parents to the new root
                    while (newRootObject != currentRoot && newRootObject != null)
                    {
                        if (currentRoot.transform.parent != null && currentRoot.transform.parent.gameObject != newRootObject) // checks if there is a parent and that it isn't the new root (don't want to add the new root)
                        {
                            currentRoot = currentRoot.transform.parent.gameObject; // gets the parent
                            newPath = currentRoot.name + "/" + newPath; // update new path
                        }
                        else
                        {
                            currentRoot = newRootObject; // stops while loop if there isn't a parent (acts as a way to prevent infinite loop if root isn't parent of bone)
                        }

                    }

                }

                // adds new path to the list of paths
                paths.Add(newPath);
                // restarts the loop and sets makingPath = false
                if (i == (bindingCount - 1))
                {
                    makingPath = false;
                    i = -1;
                }
            }
            else // updating old paths
            {
                if (typeOfUpdate == 0)
                    updateCurves(clip, bindingTemp, paths, i);
                else if (typeOfUpdate == 1)
                    updateObjectCurves(clip, bindingTemp, paths, i);
            }
        }
    }

    private void updateCurves(AnimationClip clip, UnityEditor.EditorCurveBinding bindingTemp, ArrayList paths, int i)
    {
        string tempPath = bindingTemp.path;
        AnimationCurve tempCurve = AnimationUtility.GetEditorCurve(clip, bindingTemp);
        bindingTemp.path = paths[i] as string; // sets the new path for the curve
        AnimationUtility.SetEditorCurve(clip, bindingTemp, tempCurve); // adds curve with new path
        if (bindingTemp.path != tempPath) // prevents removing curve if path was the same, as setting earlier wouldn't have added a new curve
        {
            bindingTemp.path = tempPath;
            AnimationUtility.SetEditorCurve(clip, bindingTemp, null); // removes original curve
        }
    }

    private void updateObjectCurves(AnimationClip clip, UnityEditor.EditorCurveBinding bindingTemp, ArrayList paths, int i)
    {
        string tempPath = bindingTemp.path;
        ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(clip, bindingTemp);
        bindingTemp.path = paths[i] as string; // sets the new path for the curve
        AnimationUtility.SetObjectReferenceCurve(clip, bindingTemp, keyframes); // adds curve with new path
        if (bindingTemp.path != tempPath) // prevents removing curve if path was the same, as setting earlier wouldn't have added a new curve
        {
            bindingTemp.path = tempPath;
            AnimationUtility.SetObjectReferenceCurve(clip, bindingTemp, null); // removes original curve
        }
    }

    private void showInstructions()
    {
        // sets up the string
        string t = "";
        t = t + "Instructions:------------------------------------\n";
        t = t + "-Place the animation clip you want to edit from your assets tab into the \"Animation Clip\" slot (recommended to make a copy in case edits are not liked)\n" +
            "-Place what you want the new root from your scene to be in the \"Root Object\" slot (likely the empty containing the armature and avatar)\n" +
            "-Place a game object from the scene to the \"Optional Parent\" slot if similar and/or duplicate armatures are in the scene\n" +
            "-Place an object from your scene to be in the \"Fill Object\" slot if you want animations that target location, rotation, or scale directly to have an object in their path (useful if an animator cannot be applied to object that needs animating)\n" +
            "-Hit \"Rebind Curves\" to try and apply the new root and fill object to the curves of the animation clip\n" +
            "-Hit \"Rebind Object Reference Curves\" to try and apply the new root and fill object to the object curves of the animation clip\n" +
            "-Hit \"Clear Paths\" to clear the paths for both curves and object reference curves\n" +
            "-Hit \"Instructions/Notes toggle\" to toggle the instructions you are looking at now\n";
        t = t + "Notes:------------------------------------\n";
        t = t + "-Bones must be uniquely named under armature\n" +
            "-Optional parent should be filled in with the armature the bones are under or some other parent of them if there are simlar or duplicate armatures in the scene\n" +
            "-Objects must be enabled/active to be findable by the script. (Greyed out things in the scene are not active)\n" +
            "-Should make a duplicate of your clips before editing in case the changes are not liked (press Control + D with an asset selected to duplicate in Unity)\n";
        // shows the string in a scroll view
        scrollPos3 = EditorGUILayout.BeginScrollView(scrollPos3, GUILayout.Width(300), GUILayout.Height(300));
        GUILayout.Label(t);
        EditorGUILayout.EndScrollView();
    }

    private Transform findChildRecursive(Transform x, string childName)
    {
        Transform tempTrans = x.Find(childName);
        if (tempTrans == null)
        {

            // calls recursively on each child
            for (int i = 0; i < x.childCount; i++)
            {
                Transform j = x.GetChild(i);
                Transform tempTrans2 = findChildRecursive(j, childName);
                if (tempTrans2 != null)
                {
                    tempTrans = tempTrans2;
                }
            }

        }
        return tempTrans;
    }
}