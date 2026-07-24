import { useRoller } from './state/useRoller';
import { RollerPanel } from './components/RollerPanel';
import { ToastProvider } from './components/ToastProvider';
import './App.css';

function App() {
  const roller = useRoller();

  return (
    <ToastProvider>
      <main className="app">
        <h1 className="app-title">Dice Roller</h1>
        <RollerPanel roller={roller} />
      </main>
    </ToastProvider>
  );
}

export default App;
