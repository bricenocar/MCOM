declare interface IScanRequestCommandSetStrings {
  Command1: string;
  Command2: string;
}

declare module 'ScanRequestCommandSetStrings' {
  const strings: IScanRequestCommandSetStrings;
  export = strings;
}
