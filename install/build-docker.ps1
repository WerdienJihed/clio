# CREATE CONTAINER
cd ..\clio\bin\Debug\net6.0;
docker build -f ..\..\..\..\install\DockerfileK -t atf/clio:latest .;

# RUN CONTAINER
docker run -it --rm atf/clio:latest;
