namespace Rezber.Core.Settings;

public class SwaggerSettings
{
    public static MemoryStream GetProtectedSwaggerHtml()
    {
        var html = @"
            <!DOCTYPE html>
            <html>
            <head>
                <title>Swagger UI - Rezber News API</title>
                <style>
                    .auth-container {
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        height: 100vh;
                        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                    }
                    .auth-box {
                        background: white;
                        padding: 40px;
                        border-radius: 10px;
                        box-shadow: 0 10px 40px rgba(0,0,0,0.2);
                        text-align: center;
                    }
                    .auth-box input {
                        padding: 12px;
                        margin: 10px 0;
                        width: 250px;
                        border: 1px solid #ddd;
                        border-radius: 5px;
                    }
                    .auth-box button {
                        padding: 12px 30px;
                        background: #667eea;
                        color: white;
                        border: none;
                        border-radius: 5px;
                        cursor: pointer;
                    }
                    .error {
                        color: red;
                        margin-top: 10px;
                    }
                </style>
                <script>
                    function authenticate() {
                        var password = document.getElementById('password').value;
                        if (password === 'Rezber@2024Secure!') {
                            localStorage.setItem('swagger_auth', 'true');
                            location.reload();
                        } else {
                            document.getElementById('error').innerHTML = 'Incorrect password';
                        }
                    }
                    
                    if (!localStorage.getItem('swagger_auth')) {
                        document.write(`
                            <div class='auth-container'>
                                <div class='auth-box'>
                                    <h2>Swagger UI Protected</h2>
                                    <p>Please enter the password</p>
                                    <input type='password' id='password' placeholder='Enter Password' />
                                    <br/>
                                    <button onclick='authenticate()'>Sign in</button>
                                    <div id='error' class='error'></div>
                                </div>
                            </div>
                        `);
                    }
                </script>
            </head>
            <body></body>
            </html>
        ";

        return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(html));
    }
}

