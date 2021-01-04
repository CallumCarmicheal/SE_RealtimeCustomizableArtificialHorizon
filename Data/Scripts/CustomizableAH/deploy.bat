SET F=%appdata%\SpaceEngineers\Mods\CustomizableAH
SET SRC=%F%\Data\Scripts\CustomizableAH

IF EXIST "%SRC%" RMDIR /S /Q "%SRC%"
IF EXIST "%SRC%\Gwindalmir\" RMDIR /S /Q "%SRC%\Gwindalmir\"

mkdir "%SRC%" 2> NUL
copy "..\..\..\metadata.mod" "%F%"

copy Gwindalmir\TSSBlock.cs "%SRC%\Gwindalmir\TSSBlock.cs"
copy Gwindalmir\TSSCommon.cs "%SRC%\Gwindalmir\TSSCommon.cs"

copy Logger.cs "%SRC%\Logger.cs"
copy TSSArtificialHorizon.cs "%SRC%\TSSArtificialHorizon.cs.cs"

copy TSSGravity.cs "%SRC%\TSSGravity.cs"
copy TSSVelocity.cs "%SRC%\TSSVelocity.cs"