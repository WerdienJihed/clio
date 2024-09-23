$DOCS_FOLDER_PATH = "C:/inetpub/wwwroot/ClioApiTests";
$ALLURE_RESULTS = "./clio.E2E/bin/Debug/net8.0/allure-results";
$TEST_CATEGORY = "Command"

dotnet test ./clio.E2E/clio.E2E.csproj --filter TestCategory=$TEST_CATEGORY;

# Generated report includes history folder.
# To preserve history folder, generate report in a temp dir and the copy history folder to the final destination. 

$HISORY_FOLDER = "$($DOCS_FOLDER_PATH)/history";
if(Test-Path -Path $HISORY_FOLDER){
    Copy-Item -Path $HISORY_FOLDER `
        -Destination $ALLURE_RESULTS -Recurse -Force;
}
allure generate $ALLURE_RESULTS/ -o $DOCS_FOLDER_PATH --clean;