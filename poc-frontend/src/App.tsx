import React, { useState } from 'react';
import './App.css';

function App() {
  const [sessionId, setSessionId] = useState<string | null>(null);
  const [loginStatus, setLoginStatus] = useState<string>('');
  const [weather, setWeather] = useState<any>(null);

  
  const handleLogin = async () => {
    setLoginStatus('');
    const formData = new FormData();
    formData.append('username', 'artur.karbone@gmail.com'); // hardcoded for demo
    formData.append('password', 'Deltron3030'); // hardcoded for demo
    window.location.href = 'http://localhost:5198/tokenhandler/login/pkce';
  };

    const handleWeather = async () => {
      // Check if sessionId exists in state and cookies
      const getCookie = (name: string) => {
        const match = document.cookie.match(new RegExp('(^| )' + name + '=([^;]+)'));
        return match ? match[2] : null;
      };
      // Ensure fetch sends cookies (including HttpOnly session-id) by setting credentials: 'include'
      const cookieSessionId = getCookie('session-id');

      if (!sessionId && !cookieSessionId) {
        setWeather('No session. Please login first.');
        return;
      }
      const token = sessionId || cookieSessionId;


    // Call weather API with Bearer token
    const weatherResp = await fetch('http://localhost:5198/weatherforecast', {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!weatherResp.ok) {
      setWeather('Weather API call failed.');
      return;
    }
    const weatherData = await weatherResp.json();
    setWeather(weatherData);
  };

  return (
    <div style={{ padding: 40 }}>
      <h2>POC Simple Frontend</h2>
      <button onClick={handleLogin}>Login</button>
      <span style={{ marginLeft: 10 }}>{loginStatus}</span>
      <br /><br />
      <button onClick={handleWeather}>Weather Forecast</button>
      <div style={{ marginTop: 20 }}>
        {weather && (
          <pre>{typeof weather === 'string' ? weather : JSON.stringify(weather, null, 2)}</pre>
        )}
      </div>
    </div>
  );
}

export default App;
