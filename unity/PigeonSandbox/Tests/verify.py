"""Compile Unity sources and exercise pure AI without starting the Editor."""
import os
from pathlib import Path
import subprocess
import tempfile

project = Path(__file__).resolve().parents[1]
editor = Path(os.environ.get('UNITY_EDITOR_CONTENTS', '/Applications/Unity/Hub/Editor/6000.3.11f1/Unity.app/Contents'))
scripting = editor / 'Resources/Scripting'
mono = scripting / 'MonoBleedingEdge/bin/mono'
csc = scripting / 'MonoBleedingEdge/lib/mono/4.5/csc.exe'
with tempfile.TemporaryDirectory(prefix='pigeon-checks-') as temporary:
    output = Path(temporary)
    sources = sorted(p for p in (project / 'Assets').rglob('*.cs') if 'Editor' not in p.parts)
    references = sorted((scripting / 'Managed/UnityEngine').glob('*.dll'))
    references.append(scripting / 'MonoBleedingEdge/lib/mono/4.5/Facades/netstandard.dll')
    subprocess.run([str(mono), str(csc), '-nologo', '-target:library', '-out:' + str(output / 'PigeonSandbox.dll')]
                   + ['-r:' + str(p) for p in references] + [str(p) for p in sources], check=True)
    print('PASS: Unity runtime and Editor C# compilation', flush=True)
    executable = output / 'SimulationChecks.exe'
    subprocess.run([str(mono), str(csc), '-nologo', '-out:' + str(executable),
                    str(project / 'Assets/PigeonSandbox/Core/PigeonSimulation.cs'),
                    str(project / 'Tests/SimulationChecks.cs')], check=True)
    subprocess.run([str(mono), str(executable)], check=True)
    town_executable = output / 'TownChecks.exe'
    subprocess.run([str(mono), str(csc), '-nologo', '-out:' + str(town_executable),
                    str(project / 'Assets/PigeonSandbox/Core/TownSimulation.cs'),
                    str(project / 'Tests/TownChecks.cs')], check=True)
    subprocess.run([str(mono), str(town_executable)], check=True)
