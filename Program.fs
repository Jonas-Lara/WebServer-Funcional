open System.Net.Sockets

type A = System.Net.Sockets.SocketAsyncEventArgs
type B = System.ArraySegment<byte>

exception SocketIssue of SocketError with
    override this.ToString() =
        string this.Data0

/// Envuelve la lógica Socket.xxxAsync en la lógica asíncrona de F#.
let inline asyncDo (op: A -> bool) (prepare: A -> unit)
    (select: A -> 'T) =
    Async.FromContinuations <| fun (ok, error, _) ->
        let args = new A()
        prepare args
        let k (args: A) =
            match args.SocketError with
            | System.Net.Sockets.SocketError.Success ->
                let result = select args
                args.Dispose()
                ok result
            | e ->
                args.Dispose()
                error (SocketIssue e)
        args.add_Completed(System.EventHandler<_>(fun _ -> k))
        if not (op args) then
            k args

/// Prepara los argumentos configurando el búfer.
let inline setBuffer (buf: B) (args: A) =
    args.SetBuffer(buf.Array, buf.Offset, buf.Count)

let Accept (socket: Socket) =
    asyncDo socket.AcceptAsync ignore (fun a -> a.AcceptSocket)

let Receive (socket: Socket) (buf: B) =
    asyncDo socket.ReceiveAsync (setBuffer buf)
        (fun a -> a.BytesTransferred)

let Send (socket: Socket) (buf: B) =
    asyncDo socket.SendAsync (setBuffer buf) ignore


