'use strict';
// Synchronous adapter for the existing offline editor. The native side only
// accepts requests from the bundled index.html main frame.
window.Lume=Object.freeze(Object.fromEntries([
 'loadLibrary','saveLibrary','languagePreference','deviceLanguage','saveLanguage',
 'preferredDevice','rememberDevice','discover','discoverAutomatic','wifi','cancel',
 'configure','command','upload','exportLibrary','importLibrary','copyDiagnostic',
 'previewRecovery','startPreview','previewLevels','stopPreview'
].map(method=>[method,(...args)=>{
 const response=window.prompt('__LUME_NATIVE__',JSON.stringify({method,args}));
 if(response===null)throw Error('Native bridge unavailable');
 const result=JSON.parse(response);
 if(result.error)throw Error(result.error);
 if(!Object.hasOwn(result,'value'))throw Error('Invalid native response');
 return result.value;
}])));
