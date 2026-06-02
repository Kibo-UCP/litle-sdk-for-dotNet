FROM 542216209467.dkr.ecr.us-east-1.amazonaws.com/kibo/base-images:dotnet-10-build-1 AS build



WORKDIR /src/LitleSdkForNet
COPY ["LitleSdkForNet/LitleSdkForNet.sln", "./"]
COPY ["LitleSdkForNet/LitleSdkForNet/LitleSdkForNet.csproj", "LitleSdkForNet/"]
COPY ["LitleSdkForNet/LitleSdkForNetTest/LitleSdkForNetTest.csproj", "LitleSdkForNetTest/"]

RUN dotnet restore  --source https://api.nuget.org/v3/index.json --source https://nexus.kibo-dev-ext.com/repository/nuget-localbuild/  LitleSdkForNet.sln
WORKDIR /src
COPY . .
WORKDIR /src/LitleSdkForNet
RUN bash ./sonarscanner/sonarnet.sh start || true
ARG BUILD_VER=0.0.0-alphagit
ENV BUILD_VER=$BUILD_VER
RUN dotnet build /p:Version=${BUILD_VER}  ./LitleSdkForNet.sln  -c Release --no-restore &&\
	(dotnet test ./LitleSdkForNetTest/LitleSdkForNetTest.csproj --framework net10.0 --results-directory /buildoutput/testoutput/LitleSdkForNetTest -l kibo-junit --no-build -c Release --no-restore --collect:"XPlat Code Coverage" --filter "FullyQualifiedName~Unit" || true) && \
	dotnet pack -c Release --no-build --no-restore --include-symbols /p:Version=${BUILD_VER} -o /buildoutput/nugs ./LitleSdkForNet.sln
