declare interface IScanRequestActionCommandSetStrings {
  Command1: string;
  Command2: string;
}

declare module 'ScanRequestActionCommandSetStrings' {
  const strings: IScanRequestActionCommandSetStrings;
  export = strings;
}
