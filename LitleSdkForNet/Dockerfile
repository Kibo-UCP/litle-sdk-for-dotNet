FROM dotnet-10-build-1

WORKDIR /src

COPY LitleSdkForNet/LitleSdkForNet.csproj LitleSdkForNet/
COPY LitleSdkForNetTest/LitleSdkForNetTest.csproj LitleSdkForNetTest/
COPY LitleSdkForNet.sln .

RUN dotnet restore

COPY . .

RUN dotnet build --configuration Release --no-restore

RUN dotnet test LitleSdkForNetTest/LitleSdkForNetTest.csproj \
    --configuration Release \
    --no-build \
    --filter "FullyQualifiedName~Unit" \
    --logger "junit;LogFilePath=/buildoutput/test-results/results.xml" \
    --collect:"XPlat Code Coverage" \
    --results-directory /buildoutput/test-results \
    || true

RUN dotnet pack LitleSdkForNet/LitleSdkForNet.csproj \
    --configuration Release \
    --no-build \
    --output /buildoutput/nugs
