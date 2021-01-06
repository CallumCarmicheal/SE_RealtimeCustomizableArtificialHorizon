SET F=%appdata%\SpaceEngineers\Mods\CustomizableAH
SET SRC=%F%\Data\Scripts\CustomizableAH

IF EXIST "%F%" RMDIR /S /Q "%F%"

mkdir "%SRC%" 2> NUL
mkdir "%SRC%\Gwindalmir\" 2> NUL
copy "..\..\..\modinfo.sbmi" "%F%"
copy "..\..\..\metadata.mod" "%F%"
copy "..\..\..\thumb.jpg" "%F%"

copy CustomizableAH.csproj "%SRC%\CustomizableAH.csproj"

copy Gwindalmir\TSSBlock.cs "%SRC%\Gwindalmir\TSSBlock.cs"
copy Gwindalmir\TSSCommon.cs "%SRC%\Gwindalmir\TSSCommon.cs"

copy AHConfig.cs "%SRC%\AHConfig.cs"
copy Logger.cs "%SRC%\Logger.cs"
copy TSSArtificialHorizon.cs "%SRC%\TSSArtificialHorizon.cs"

copy TSSGravity.cs "%SRC%\TSSGravity.cs"
copy TSSVelocity.cs "%SRC%\TSSVelocity.cs"