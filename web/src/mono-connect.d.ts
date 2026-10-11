declare module '@mono.co/connect.js' {
  interface MonoConnectOptions {
    key: string
    scope?: string
    data?: { customer: { name?: string; email?: string; id?: string } }
    onSuccess: (data: { code: string }) => void
    onClose?: () => void
    onLoad?: () => void
    onEvent?: (eventName: string, data: unknown) => void
    reference?: string
  }
  export default class Connect {
    constructor(options: MonoConnectOptions)
    setup(): void
    open(): void
    close(): void
  }
}
