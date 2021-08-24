% Read the text file and use jsondecode to produce a cell array of
% structures.

J = jsondecode(fileread("Application_Log.json"));

% The structures will have different fields, so we must use loops to filter
% them as they cannot be combined into a single struct array or table.

% Below, find all the events of type SpawnObject, and combine them into a
% new array.

spawn = [];

for j = J'
   % The curly braces access the contents of the cell j, which is the
   % struct.
   s = j{1};
    if categorical(cellstr(s.event)) == categorical("SpawnObject")
        spawn = [spawn; s];
    end
end

% Convert the new array into a table
T = struct2table(spawn);

% Use the table to change the type of the sceneid column so we can easily
% split the events by which peer they are from.
T.sceneid = categorical(T.sceneid);

% Filter the events to keep only those at the spawner
T = T(T.arg3,:);

% Plot the number of objects spawned over time, for each Peer
figure;
hold all;
peers = unique(T.sceneid);
for p = peers'
   spawned = T(T.sceneid == p,:);
   plot(spawned.ticks,1:size(spawned,1));
end

xlabel("Time (ticks)");
ylabel("Number of Objects");
legend(peers);