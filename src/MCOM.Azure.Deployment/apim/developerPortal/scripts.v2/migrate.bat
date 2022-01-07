@REM This script automates content migration between developer portal instances.

node ./migrate ^
--sourceEndpoint "apim-eim-dev.management.azure-api.net" ^
--sourceToken "SharedAccessSignature integration&202105231047&7NOLhTRHYAPoRtdOvFfUzbrzo/xyroSWD8H+G12hzrE/Q0lGo9uSxJ7oN3XiTz7K3GWspfM37uS0lAj74GqNZg==" ^
--destEndpoint "apim-eimarchiving-devtst.management.azure-api.net" ^
--destToken "SharedAccessSignature integration&202105231145&6mmMAfGfSH04DOtstuHdnkBRd0zST0FeQiJHYYgtbFWzIwMh8hj877BhHldfllaxygTvDrs9BjyGH16fRe5Jfw==" ^

