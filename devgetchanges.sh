#!/bin/bash

cd ~

rm -fR Documents/workspace/microting/eform-angular-greate-belt-plugin/eform-client/src/app/plugins/modules/greate-belt-pn

cp -a Documents/workspace/microting/eform-angular-frontend/eform-client/src/app/plugins/modules/greate-belt-pn Documents/workspace/microting/eform-angular-greate-belt-plugin/eform-client/src/app/plugins/modules/greate-belt-pn

rm -fR Documents/workspace/microting/eform-angular-greate-belt-plugin/eFormAPI/Plugins/GreateBelt.Pn

cp -a Documents/workspace/microting/eform-angular-frontend/eFormAPI/Plugins/GreateBelt.Pn Documents/workspace/microting/eform-angular-greate-belt-plugin/eFormAPI/Plugins/GreateBelt.Pn

# Test files rm
rm -fR Documents/workspace/microting/eform-angular-greate-belt-plugin/eform-client/e2e/Tests/greate-belt-settings/
rm -fR Documents/workspace/microting/eform-angular-greate-belt-plugin/eform-client/e2e/Tests/greate-belt-general/
rm -fR Documents/workspace/microting/eform-angular-greate-belt-plugin/eform-client/wdio-headless-plugin-step2.conf.js
rm -fR Documents/workspace/microting/eform-angular-greate-belt-plugin/eform-client/e2e/Page\ objects/GreateBelt

# Test files cp
cp -a Documents/workspace/microting/eform-angular-frontend/eform-client/e2e/Tests/greate-belt-settings Documents/workspace/microting/eform-angular-greate-belt-plugin/eform-client/e2e/Tests/greate-belt-settings
cp -a Documents/workspace/microting/eform-angular-frontend/eform-client/e2e/Tests/greate-belt-general Documents/workspace/microting/eform-angular-greate-belt-plugin/eform-client/e2e/Tests/greate-belt-general
cp -a Documents/workspace/microting/eform-angular-frontend/eform-client/wdio-plugin-step2.conf.js Documents/workspace/microting/eform-angular-greate-belt-plugin/eform-client/wdio-headless-plugin-step2.conf.js
cp -a Documents/workspace/microting/eform-angular-frontend/eform-client/e2e/Page\ objects/GreateBelt Documents/workspace/microting/eform-angular-greate-belt-plugin/eform-client/e2e/Page\ objects/GreateBelt
