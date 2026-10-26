# CAP 5118 Replication Project

The shared industrial-environment project contains the rotation, translation and curvature experiments. The `main` branch combines the team's scene updates with the rotation pilot implementation.

## Run

Open the repository root in Unity **6000.5.6f1**, let packages import, and open the appropriate scene:

| Experiment | Scene | Subgroup |
| --- | --- | --- |
| Rotation | `Assets/Scenes/Rotation.unity` | Rohan Gudla and Alix Fyall |
| Translation | `Assets/Scenes/Translation.unity` | Logan Georgi and Christian Wagenknecht |
| Curvature | `Assets/Scenes/Curvature.unity` | Anthony Aubert and Lucas Mcclean |

Connect Quest 3 through Link for PC testing, start Play, and enter the participant ID assigned for the session. Use the same ID in the questionnaire. The common response scale is -3 through +3, including zero. Each mode retains its own task and instructions.

Rotation includes explicit instructions, five practice and eighteen measured trials, labeled slower/same/faster answers, and unique per-session recording. See [the rotation setup](README_ROTATION_PILOT.md) and [facilitator script](docs/ROTATION_FACILITATOR_SCRIPT.md).

The translation and curvature scene/slide changes and their existing tracking and export behavior are preserved from the team's latest main branch. See [integration notes](docs/INTEGRATION_REVIEW.md).

## Software checks

After Unity imports the project, run `Tools/RotationPilot/Check-Software.ps1` from PowerShell. It compiles the runtime and Editor scripts, runs ten independent recorder checks and 441 managed integration assertions against the actual trial/input/logging source for all three modes. The managed integration harness uses small API test doubles; it does not verify rendering or headset tracking.

For scene-level software QA, save the scene and choose **Tools > Rotation Pilot > Run combined project integration QA** in the licensed Unity Editor. Generated QA data is explicitly synthetic and excluded from Git. Participant datasets stay outside source control.

The rotation version was physically pilot-tested on Quest 3 through Link on October 24. Integration compilation, recorder and managed state-machine checks passed on October 26. A fresh combined Unity scene run was attempted on October 26 but could not start because the local Editor license was unavailable; it is not reported as passed. These checks do not represent advisor approval or assignment submission.
