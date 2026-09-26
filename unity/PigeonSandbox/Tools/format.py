"""Format Unity C# sources with the Roslyn bundled in Unity. Use --check to only report."""
import os
from pathlib import Path
import subprocess
import sys
import tempfile

project = Path(__file__).resolve().parents[1]
editor = Path(os.environ.get('UNITY_EDITOR_CONTENTS', '/Applications/Unity/Hub/Editor/6000.3.11f1/Unity.app/Contents'))
mono_root = editor / 'Resources/Scripting/MonoBleedingEdge'
mono = mono_root / 'bin/mono'
lib = mono_root / 'lib/mono/4.5'
sources = sorted(p for folder in ('Assets', 'Tests') for p in (project / folder).rglob('*.cs'))
with tempfile.TemporaryDirectory(prefix='pigeon-format-') as temporary:
    tool = Path(temporary) / 'Formatter.exe'
    subprocess.run([str(mono), str(lib / 'csc.exe'), '-nologo', '-out:' + str(tool),
                    '-r:' + str(lib / 'Microsoft.CodeAnalysis.dll'), '-r:' + str(lib / 'Microsoft.CodeAnalysis.CSharp.dll'),
                    '-r:' + str(lib / 'Facades/netstandard.dll'), '-r:' + str(lib / 'System.Collections.Immutable.dll'),
                    str(project / 'Tools/Formatter.cs')], check=True)
    mode = ['--check'] if '--check' in sys.argv[1:] else []
    result = subprocess.run([str(mono), str(tool)] + mode + [str(p) for p in sources], env={**os.environ, 'MONO_PATH': str(lib)})
    sys.exit(result.returncode)
